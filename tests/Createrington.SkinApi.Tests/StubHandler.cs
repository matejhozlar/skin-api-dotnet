using System.Net.Http.Headers;
using System.Text;

// Env-key tests mutate a process-global environment variable; serialize the
// suite so they cannot race other tests.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Createrington.SkinApi.Tests;

internal sealed record CapturedRequest(
    HttpMethod Method,
    Uri Uri,
    string? Authorization,
    string? UserAgent,
    byte[] Body,
    string? ContentType)
{
    public string BodyText => Encoding.UTF8.GetString(Body);
}

internal sealed class StubHandler : HttpMessageHandler
{
    private readonly Func<int, HttpResponseMessage> _responder;
    private int _count;

    public List<CapturedRequest> Requests { get; } = new();

    public StubHandler(Func<int, HttpResponseMessage> responder) => _responder = responder;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content is null
            ? Array.Empty<byte>()
            : await request.Content.ReadAsByteArrayAsync(cancellationToken);

        var userAgent = request.Headers.TryGetValues("User-Agent", out var ua)
            ? string.Join(" ", ua)
            : null;

        Requests.Add(new CapturedRequest(
            request.Method,
            request.RequestUri!,
            request.Headers.Authorization?.ToString(),
            userAgent,
            body,
            request.Content?.Headers.ContentType?.ToString()));

        return _responder(_count++);
    }

    public static HttpResponseMessage Png()
    {
        var content = new ByteArrayContent(TestData.PngBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = content };
    }

    public static HttpResponseMessage Json(System.Net.HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}

internal static class TestData
{
    public static readonly byte[] PngBytes =
        { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00 };
}
