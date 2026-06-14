using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace Createrington.SkinApi;

/// <summary>
/// Client for the Createrington Skin API. Renders Minecraft player skins into
/// named poses and returns PNG bytes.
/// </summary>
public sealed class SkinApiClient : IDisposable
{
    private const string ApiKeyEnvVar = "SKIN_API_KEY";

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly TimeSpan _timeout;
    private readonly int _retries;
    private readonly string _userAgent;

    /// <summary>
    /// Creates a client with an internally managed <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="apiKey">
    /// The API key. When null, falls back to the <c>SKIN_API_KEY</c> environment variable.
    /// </param>
    /// <param name="options">Optional client configuration.</param>
    public SkinApiClient(string? apiKey = null, SkinApiClientOptions? options = null)
        : this(httpClient: null, apiKey, options, ownsHttpClient: true)
    {
    }

    /// <summary>
    /// Creates a client over a caller-supplied <see cref="HttpClient"/>, e.g. one
    /// obtained from <c>IHttpClientFactory</c>. The supplied client is not disposed
    /// by this instance and its default headers are left untouched.
    /// </summary>
    /// <param name="httpClient">The HTTP client to send requests with.</param>
    /// <param name="apiKey">
    /// The API key. When null, falls back to the <c>SKIN_API_KEY</c> environment variable.
    /// </param>
    /// <param name="options">Optional client configuration.</param>
    public SkinApiClient(HttpClient httpClient, string? apiKey = null, SkinApiClientOptions? options = null)
        : this(httpClient ?? throw new ArgumentNullException(nameof(httpClient)), apiKey, options, ownsHttpClient: false)
    {
    }

    private SkinApiClient(HttpClient? httpClient, string? apiKey, SkinApiClientOptions? options, bool ownsHttpClient)
    {
        var key = apiKey ?? Environment.GetEnvironmentVariable(ApiKeyEnvVar);
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException(
                "api_key is required: pass apiKey or set the SKIN_API_KEY environment variable.",
                nameof(apiKey));
        }

        options ??= new SkinApiClientOptions();

        if (options.Timeout <= TimeSpan.Zero && options.Timeout != System.Threading.Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options), options.Timeout, "Timeout must be positive or Timeout.InfiniteTimeSpan.");
        }

        if (options.Retries < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options), options.Retries, "Retries must be non-negative.");
        }

        _apiKey = key;
        _ownsHttpClient = ownsHttpClient;
        _httpClient = httpClient ?? new HttpClient();
        _baseUrl = options.BaseUrl.ToString().TrimEnd('/');
        _timeout = options.Timeout;
        _retries = options.Retries;
        _userAgent = options.UserAgent;

        // We manage the per-request deadline via a linked CTS so a timeout is
        // distinguishable from a caller cancellation. Disable HttpClient's own
        // timeout only when we own the instance.
        if (_ownsHttpClient)
        {
            _httpClient.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
        }
    }

    /// <summary>
    /// Renders <paramref name="pose"/> for the given skin source and returns the PNG bytes.
    /// </summary>
    /// <param name="pose">A pose name (see <see cref="KnownPoses"/>); any string is accepted.</param>
    /// <param name="source">The skin source, created via a <see cref="SkinSource"/> factory.</param>
    /// <param name="options">Optional render parameters.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The rendered PNG image bytes.</returns>
    /// <exception cref="SkinApiException">The request failed after exhausting retries.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    public async Task<byte[]> RenderAsync(
        string pose,
        SkinSource source,
        RenderOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(pose);
        ArgumentNullException.ThrowIfNull(source);

        var requestUri = BuildRenderRequestUri(pose, source, options);
        return await SendAsync(requestUri, source, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Renders a flat 2D front-view avatar (the head's face with the hat layer
    /// composited on top) for the given skin source and returns the PNG bytes.
    /// </summary>
    /// <param name="source">The skin source, created via a <see cref="SkinSource"/> factory.</param>
    /// <param name="options">Optional avatar parameters.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The avatar PNG image bytes.</returns>
    /// <exception cref="SkinApiException">The request failed after exhausting retries.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    public async Task<byte[]> AvatarAsync(
        SkinSource source,
        AvatarOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        var requestUri = BuildAvatarRequestUri(source, options);
        return await SendAsync(requestUri, source, cancellationToken).ConfigureAwait(false);
    }

    private async Task<byte[]> SendAsync(
        Uri requestUri,
        SkinSource source,
        CancellationToken cancellationToken)
    {
        var method = source.IsQuerySource ? HttpMethod.Get : HttpMethod.Post;

        var attempt = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_timeout);

            using var request = new HttpRequestMessage(method, requestUri)
            {
                Content = source.IsQuerySource ? null : source.CreateContent(),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Headers.TryAddWithoutValidation("User-Agent", _userAgent);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient
                    .SendAsync(request, HttpCompletionOption.ResponseContentRead, timeoutCts.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException ex)
            {
                if (attempt < _retries)
                {
                    await DelayAsync(RetryPolicy.Backoff(attempt, Random.Shared.NextDouble()), cancellationToken)
                        .ConfigureAwait(false);
                    attempt++;
                    continue;
                }

                var seconds = _timeout.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture);
                throw new SkinApiException(
                    $"Request timed out after {seconds}s", SkinApiErrorCode.Timeout, 0, null, ex);
            }
            catch (HttpRequestException ex)
            {
                if (attempt < _retries)
                {
                    await DelayAsync(RetryPolicy.Backoff(attempt, Random.Shared.NextDouble()), cancellationToken)
                        .ConfigureAwait(false);
                    attempt++;
                    continue;
                }

                throw new SkinApiException(ex.Message, SkinApiErrorCode.NetworkError, 0, null, ex);
            }

            try
            {
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                }

                var status = (int)response.StatusCode;
                var body = await ReadBodySafelyAsync(response, cancellationToken).ConfigureAwait(false);
                var parsed = SkinApiException.ParseBody(body);

                if (RetryPolicy.IsRetryableStatus(status) && attempt < _retries)
                {
                    var delay = RetryPolicy.RetryDelay(
                        status, parsed.RetryAfterMs, attempt, Random.Shared.NextDouble());
                    await DelayAsync(delay, cancellationToken).ConfigureAwait(false);
                    attempt++;
                    continue;
                }

                throw SkinApiException.FromParsed(status, parsed);
            }
            finally
            {
                response.Dispose();
            }
        }
    }

    private Uri BuildRenderRequestUri(string pose, SkinSource source, RenderOptions? options)
    {
        var query = new StringBuilder("pose=").Append(Uri.EscapeDataString(pose));
        AppendQuerySource(query, source);

        if (options?.Slim is bool slim)
        {
            query.Append("&slim=").Append(slim ? "true" : "false");
        }

        // Omit entirely unless on, matching the server default and keeping the
        // render cache key unchanged for non-outline calls.
        if (options?.Outline is true)
        {
            query.Append("&outline=true");
        }

        if (options?.Width is int width)
        {
            query.Append("&width=").Append(width.ToString(CultureInfo.InvariantCulture));
        }

        if (options?.Height is int height)
        {
            query.Append("&height=").Append(height.ToString(CultureInfo.InvariantCulture));
        }

        return new Uri($"{_baseUrl}/v1/render?{query}");
    }

    private Uri BuildAvatarRequestUri(SkinSource source, AvatarOptions? options)
    {
        var query = new StringBuilder();
        if (source.IsQuerySource)
        {
            query.Append(source.QueryField).Append('=').Append(Uri.EscapeDataString(source.Value!));
        }

        if (options?.Size is int size)
        {
            if (query.Length > 0)
            {
                query.Append('&');
            }

            query.Append("size=").Append(size.ToString(CultureInfo.InvariantCulture));
        }

        if (options?.Overlay is false)
        {
            if (query.Length > 0)
            {
                query.Append('&');
            }

            query.Append("overlay=false");
        }

        return query.Length > 0
            ? new Uri($"{_baseUrl}/v1/avatar?{query}")
            : new Uri($"{_baseUrl}/v1/avatar");
    }

    private static void AppendQuerySource(StringBuilder query, SkinSource source)
    {
        if (source.IsQuerySource)
        {
            query.Append('&').Append(source.QueryField).Append('=')
                .Append(Uri.EscapeDataString(source.Value!));
        }
    }

    private static Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        Task.Delay(delay, cancellationToken);

    private static async Task<string?> ReadBodySafelyAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Disposes the internally managed <see cref="HttpClient"/>, if any.</summary>
    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}
