using System.Text.Json;

namespace Createrington.SkinApi;

/// <summary>
/// Thrown for every non-success response from the Skin API, and for
/// network or timeout failures.
/// </summary>
public sealed class SkinApiException : Exception
{
    /// <summary>The categorised error code.</summary>
    public SkinApiErrorCode Code { get; }

    /// <summary>
    /// The HTTP status code, or <c>0</c> for network and timeout failures
    /// that never received a response.
    /// </summary>
    public int Status { get; }

    /// <summary>
    /// The server-suggested delay before retrying, in milliseconds, when the
    /// server reported one on a <c>429</c> response; otherwise <see langword="null"/>.
    /// </summary>
    public int? RetryAfterMs { get; }

    /// <summary>Creates a new <see cref="SkinApiException"/>.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="code">The categorised error code.</param>
    /// <param name="status">The HTTP status code, or <c>0</c> for transport failures.</param>
    /// <param name="retryAfterMs">The server-suggested retry delay in milliseconds, if any.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public SkinApiException(
        string message,
        SkinApiErrorCode code,
        int status,
        int? retryAfterMs = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        Status = status;
        RetryAfterMs = retryAfterMs;
    }

    private static readonly Dictionary<int, SkinApiErrorCode> StatusToCode = new()
    {
        [400] = SkinApiErrorCode.BadRequest,
        [401] = SkinApiErrorCode.Unauthorized,
        [403] = SkinApiErrorCode.Forbidden,
        [404] = SkinApiErrorCode.NotFound,
        [409] = SkinApiErrorCode.Conflict,
        [415] = SkinApiErrorCode.UnsupportedMediaType,
        [429] = SkinApiErrorCode.RateLimited,
        [500] = SkinApiErrorCode.Internal,
        [502] = SkinApiErrorCode.UpstreamUnavailable,
        [503] = SkinApiErrorCode.UpstreamUnavailable,
        [504] = SkinApiErrorCode.UpstreamUnavailable,
    };

    // The server emits codes in UPPER_SNAKE (NOT_FOUND, RATE_LIMITED); map the
    // lowercased form so it survives the casing difference.
    private static readonly Dictionary<string, SkinApiErrorCode> StringToCode = new(StringComparer.Ordinal)
    {
        ["bad_request"] = SkinApiErrorCode.BadRequest,
        ["unauthorized"] = SkinApiErrorCode.Unauthorized,
        ["forbidden"] = SkinApiErrorCode.Forbidden,
        ["not_found"] = SkinApiErrorCode.NotFound,
        ["conflict"] = SkinApiErrorCode.Conflict,
        ["unsupported_media_type"] = SkinApiErrorCode.UnsupportedMediaType,
        ["rate_limited"] = SkinApiErrorCode.RateLimited,
        ["internal"] = SkinApiErrorCode.Internal,
        ["render_failed"] = SkinApiErrorCode.RenderFailed,
        ["upstream_unavailable"] = SkinApiErrorCode.UpstreamUnavailable,
        ["timeout"] = SkinApiErrorCode.Timeout,
        ["network_error"] = SkinApiErrorCode.NetworkError,
    };

    internal readonly struct ParsedError
    {
        public SkinApiErrorCode? Code { get; init; }
        public string? Message { get; init; }
        public int? RetryAfterMs { get; init; }
    }

    internal static ParsedError ParseBody(string? body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return default;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("error", out var error)
                || error.ValueKind != JsonValueKind.Object)
            {
                return default;
            }

            string? message = null;
            if (error.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
            {
                message = m.GetString();
            }

            SkinApiErrorCode? code = null;
            if (error.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.String)
            {
                var raw = c.GetString();
                if (raw is not null && StringToCode.TryGetValue(raw.ToLowerInvariant(), out var mapped))
                {
                    code = mapped;
                }
            }

            int? retryAfterMs = null;
            if (error.TryGetProperty("retryAfterMs", out var r)
                && r.ValueKind == JsonValueKind.Number
                && r.TryGetInt32(out var ms))
            {
                retryAfterMs = ms;
            }

            return new ParsedError { Code = code, Message = message, RetryAfterMs = retryAfterMs };
        }
        catch (JsonException)
        {
            return default;
        }
    }

    internal static SkinApiException FromParsed(int status, ParsedError parsed)
    {
        var code = parsed.Code
            ?? (StatusToCode.TryGetValue(status, out var mapped) ? mapped : SkinApiErrorCode.Unknown);
        return new SkinApiException(parsed.Message ?? $"HTTP {status}", code, status, parsed.RetryAfterMs);
    }
}
