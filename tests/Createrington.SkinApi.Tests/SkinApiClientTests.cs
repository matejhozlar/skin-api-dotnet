using System.Net;

namespace Createrington.SkinApi.Tests;

public sealed class SkinApiClientTests
{
    private static (SkinApiClient Client, StubHandler Handler) Make(
        Func<int, HttpResponseMessage> responder,
        int retries = 2)
    {
        var handler = new StubHandler(responder);
        var client = new SkinApiClient(
            new HttpClient(handler),
            apiKey: "test-key",
            options: new SkinApiClientOptions { BaseUrl = new Uri("http://skin.test"), Retries = retries });
        return (client, handler);
    }

    [Fact]
    public void Constructor_RequiresApiKey()
    {
        var original = Environment.GetEnvironmentVariable("SKIN_API_KEY");
        Environment.SetEnvironmentVariable("SKIN_API_KEY", null);
        try
        {
            Assert.Throws<ArgumentException>(() => new SkinApiClient());
        }
        finally
        {
            Environment.SetEnvironmentVariable("SKIN_API_KEY", original);
        }
    }

    [Fact]
    public async Task Constructor_ReadsApiKeyFromEnvironment()
    {
        var original = Environment.GetEnvironmentVariable("SKIN_API_KEY");
        Environment.SetEnvironmentVariable("SKIN_API_KEY", "env-key");
        try
        {
            var handler = new StubHandler(_ => StubHandler.Png());
            using var client = new SkinApiClient(
                new HttpClient(handler),
                options: new SkinApiClientOptions { BaseUrl = new Uri("http://skin.test") });
            await client.RenderAsync("wave", SkinSource.FromUuid("abc"));
            Assert.Equal("Bearer env-key", handler.Requests[0].Authorization);
        }
        finally
        {
            Environment.SetEnvironmentVariable("SKIN_API_KEY", original);
        }
    }

    [Fact]
    public async Task DefaultBaseUrl_IsCreaterington()
    {
        var handler = new StubHandler(_ => StubHandler.Png());
        using var client = new SkinApiClient(new HttpClient(handler), apiKey: "test-key");
        await client.RenderAsync("wave", SkinSource.FromUuid("abc"));
        Assert.StartsWith("https://api.createrington.com", handler.Requests[0].Uri.ToString());
    }

    [Fact]
    public async Task Render_Uuid_UsesGetWithQueryParamAndNoBody()
    {
        var (client, handler) = Make(_ => StubHandler.Png());
        var png = await client.RenderAsync(
            "wave",
            SkinSource.FromUuid("uuid-1"),
            new RenderOptions { Slim = true, Width = 200, Height = 300 });

        var req = handler.Requests[0];
        Assert.Equal(TestData.PngBytes, png);
        Assert.Equal(HttpMethod.Get, req.Method);
        Assert.Equal("/v1/render", req.Uri.AbsolutePath);
        Assert.Equal("?pose=wave&uuid=uuid-1&slim=true&width=200&height=300", req.Uri.Query);
        Assert.Equal("Bearer test-key", req.Authorization);
        Assert.Null(req.ContentType);
        Assert.Empty(req.Body);
    }

    [Fact]
    public async Task Render_Username_UsesGetWithQueryParamAndNoBody()
    {
        var (client, handler) = Make(_ => StubHandler.Png());
        var png = await client.RenderAsync("wave", SkinSource.FromUsername("Steve"));

        var req = handler.Requests[0];
        Assert.Equal(TestData.PngBytes, png);
        Assert.Equal(HttpMethod.Get, req.Method);
        Assert.Equal("/v1/render", req.Uri.AbsolutePath);
        Assert.Equal("?pose=wave&username=Steve", req.Uri.Query);
        Assert.Null(req.ContentType);
        Assert.Empty(req.Body);
    }

    [Fact]
    public async Task Render_OutlineTrue_SendsOutlineParam()
    {
        var (client, handler) = Make(_ => StubHandler.Png());
        await client.RenderAsync(
            "wave", SkinSource.FromUuid("x"), new RenderOptions { Outline = true });
        Assert.Equal("?pose=wave&uuid=x&outline=true", handler.Requests[0].Uri.Query);
    }

    [Fact]
    public async Task Render_OutlineFalse_OmitsOutlineParam()
    {
        var (client, handler) = Make(_ => StubHandler.Png());
        await client.RenderAsync(
            "wave", SkinSource.FromUuid("x"), new RenderOptions { Outline = false });
        Assert.Equal("?pose=wave&uuid=x", handler.Requests[0].Uri.Query);
    }

    [Fact]
    public async Task Render_OutlineUnset_OmitsOutlineParam()
    {
        var (client, handler) = Make(_ => StubHandler.Png());
        await client.RenderAsync("wave", SkinSource.FromUuid("x"), new RenderOptions());
        Assert.Equal("?pose=wave&uuid=x", handler.Requests[0].Uri.Query);
    }

    [Fact]
    public async Task Render_SetsUserAgent()
    {
        var (client, handler) = Make(_ => StubHandler.Png());
        await client.RenderAsync("wave", SkinSource.FromUuid("x"));
        Assert.Equal("createrington-skin-api", handler.Requests[0].UserAgent);
    }

    [Fact]
    public async Task Render_PngSource_UsesMultipart()
    {
        var (client, handler) = Make(_ => StubHandler.Png());
        await client.RenderAsync("wave", SkinSource.FromPng(TestData.PngBytes));
        var req = handler.Requests[0];
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.StartsWith("multipart/form-data", req.ContentType);
        Assert.Contains("skin.png", req.BodyText);
    }

    [Fact]
    public async Task Render_SkinUrl_MapsToCamelCaseField()
    {
        var (client, handler) = Make(_ => StubHandler.Png());
        await client.RenderAsync("wave", SkinSource.FromUrl("https://example.com/skin.png"));
        var req = handler.Requests[0];
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.StartsWith("application/json", req.ContentType);
        Assert.Equal("{\"skinUrl\":\"https://example.com/skin.png\"}", req.BodyText);
    }

    [Fact]
    public async Task Render_Base64_MapsToCamelCaseField()
    {
        var (client, handler) = Make(_ => StubHandler.Png());
        await client.RenderAsync("wave", SkinSource.FromBase64("AAAA"));
        var req = handler.Requests[0];
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.StartsWith("application/json", req.ContentType);
        Assert.Equal("{\"skinBase64\":\"AAAA\"}", req.BodyText);
    }

    [Fact]
    public async Task Avatar_Uuid_UsesGetWithQueryParamsAndNoBody()
    {
        var (client, handler) = Make(_ => StubHandler.Png());
        var png = await client.AvatarAsync(
            SkinSource.FromUuid("uuid-1"),
            new AvatarOptions { Size = 128, Overlay = true });

        var req = handler.Requests[0];
        Assert.Equal(TestData.PngBytes, png);
        Assert.Equal(HttpMethod.Get, req.Method);
        Assert.Equal("/v1/avatar", req.Uri.AbsolutePath);
        Assert.Equal("?uuid=uuid-1&size=128", req.Uri.Query);
        Assert.Equal("Bearer test-key", req.Authorization);
        Assert.Null(req.ContentType);
        Assert.Empty(req.Body);
    }

    [Fact]
    public async Task Avatar_Username_UsesGetWithQueryParamAndNoBody()
    {
        var (client, handler) = Make(_ => StubHandler.Png());
        var png = await client.AvatarAsync(SkinSource.FromUsername("Steve"));

        var req = handler.Requests[0];
        Assert.Equal(TestData.PngBytes, png);
        Assert.Equal(HttpMethod.Get, req.Method);
        Assert.Equal("/v1/avatar", req.Uri.AbsolutePath);
        Assert.Equal("?username=Steve", req.Uri.Query);
        Assert.Null(req.ContentType);
        Assert.Empty(req.Body);
    }

    [Fact]
    public async Task Avatar_OverlayFalse_SendsOverlayParam()
    {
        var (client, handler) = Make(_ => StubHandler.Png());
        await client.AvatarAsync(SkinSource.FromUuid("x"), new AvatarOptions { Overlay = false });
        Assert.Equal("?uuid=x&overlay=false", handler.Requests[0].Uri.Query);
    }

    [Fact]
    public async Task Avatar_NoOptions_OmitsOptionParams()
    {
        var (client, handler) = Make(_ => StubHandler.Png());
        await client.AvatarAsync(SkinSource.FromUuid("x"), new AvatarOptions());
        Assert.Equal("?uuid=x", handler.Requests[0].Uri.Query);
    }

    [Fact]
    public async Task Avatar_OutOfRangeSize_ForwardsToServerWithoutThrowing()
    {
        var (client, handler) = Make(_ => StubHandler.Png());
        await client.AvatarAsync(SkinSource.FromUuid("x"), new AvatarOptions { Size = 9999 });
        Assert.Equal("?uuid=x&size=9999", handler.Requests[0].Uri.Query);
    }

    [Fact]
    public async Task Avatar_PngSourceWithSize_UsesMultipartAndQueryOptions()
    {
        var (client, handler) = Make(_ => StubHandler.Png());
        await client.AvatarAsync(
            SkinSource.FromPng(TestData.PngBytes), new AvatarOptions { Size = 256 });
        var req = handler.Requests[0];
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.Equal("/v1/avatar", req.Uri.AbsolutePath);
        Assert.Equal("?size=256", req.Uri.Query);
        Assert.StartsWith("multipart/form-data", req.ContentType);
        Assert.Contains("skin.png", req.BodyText);
    }

    [Fact]
    public async Task Avatar_SkinUrl_MapsToCamelCaseField()
    {
        var (client, handler) = Make(_ => StubHandler.Png());
        await client.AvatarAsync(SkinSource.FromUrl("https://example.com/skin.png"));
        var req = handler.Requests[0];
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.Equal("/v1/avatar", req.Uri.AbsolutePath);
        Assert.StartsWith("application/json", req.ContentType);
        Assert.Equal("{\"skinUrl\":\"https://example.com/skin.png\"}", req.BodyText);
    }

    [Fact]
    public async Task Avatar_Base64_MapsToCamelCaseField()
    {
        var (client, handler) = Make(_ => StubHandler.Png());
        await client.AvatarAsync(SkinSource.FromBase64("AAAA"));
        var req = handler.Requests[0];
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.StartsWith("application/json", req.ContentType);
        Assert.Equal("{\"skinBase64\":\"AAAA\"}", req.BodyText);
    }

    [Fact]
    public async Task Avatar_SetsUserAgent()
    {
        var (client, handler) = Make(_ => StubHandler.Png());
        await client.AvatarAsync(SkinSource.FromUuid("x"));
        Assert.Equal("createrington-skin-api", handler.Requests[0].UserAgent);
    }

    [Fact]
    public async Task Avatar_NullSource_Throws()
    {
        var (client, _) = Make(_ => StubHandler.Png());
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.AvatarAsync(null!));
    }

    [Fact]
    public async Task Avatar_NormalizesUpperSnakeErrorCode()
    {
        var (client, _) = Make(
            _ => StubHandler.Json(HttpStatusCode.NotFound,
                "{\"error\":{\"code\":\"NOT_FOUND\",\"message\":\"Player missing\"}}"),
            retries: 0);

        var ex = await Assert.ThrowsAsync<SkinApiException>(
            () => client.AvatarAsync(SkinSource.FromUuid("x")));
        Assert.Equal(SkinApiErrorCode.NotFound, ex.Code);
        Assert.Equal(404, ex.Status);
        Assert.Equal("Player missing", ex.Message);
    }

    [Fact]
    public async Task Avatar_Retries429ThenSucceeds()
    {
        var (client, handler) = Make(
            i => i == 0
                ? StubHandler.Json((HttpStatusCode)429, "{\"error\":{\"code\":\"RATE_LIMITED\",\"retryAfterMs\":5}}")
                : StubHandler.Png(),
            retries: 1);

        var png = await client.AvatarAsync(SkinSource.FromUuid("x"));
        Assert.Equal(TestData.PngBytes, png);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Avatar_CallerCancellation_PropagatesAndDoesNotSend()
    {
        var (client, handler) = Make(_ => StubHandler.Png());
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.AvatarAsync(SkinSource.FromUuid("x"), cancellationToken: cts.Token));
        Assert.Empty(handler.Requests);
    }

    private const string ResolvedNotchJson =
        "{\"uuid\":\"069a79f4-44e9-4726-a5be-fca90e38aaf5\",\"username\":\"Notch\"}";

    [Fact]
    public async Task Resolve_Uuid_UsesGetWithQueryParamAndNoBody()
    {
        var (client, handler) = Make(_ => StubHandler.Json(HttpStatusCode.OK, ResolvedNotchJson));
        var player = await client.ResolveAsync(
            PlayerIdentifier.FromUuid("069a79f444e94726a5befca90e38aaf5"));

        var req = handler.Requests[0];
        Assert.Equal(HttpMethod.Get, req.Method);
        Assert.Equal("/v1/resolve", req.Uri.AbsolutePath);
        Assert.Equal("?uuid=069a79f444e94726a5befca90e38aaf5", req.Uri.Query);
        Assert.Equal("Bearer test-key", req.Authorization);
        Assert.Null(req.ContentType);
        Assert.Empty(req.Body);
        Assert.Equal("069a79f4-44e9-4726-a5be-fca90e38aaf5", player.Uuid);
        Assert.Equal("Notch", player.Username);
    }

    [Fact]
    public async Task Resolve_Username_UsesGetWithQueryParamAndNoBody()
    {
        var (client, handler) = Make(_ => StubHandler.Json(HttpStatusCode.OK, ResolvedNotchJson));
        var player = await client.ResolveAsync(PlayerIdentifier.FromUsername("notch"));

        var req = handler.Requests[0];
        Assert.Equal(HttpMethod.Get, req.Method);
        Assert.Equal("/v1/resolve", req.Uri.AbsolutePath);
        Assert.Equal("?username=notch", req.Uri.Query);
        Assert.Null(req.ContentType);
        Assert.Empty(req.Body);
        Assert.Equal("069a79f4-44e9-4726-a5be-fca90e38aaf5", player.Uuid);
        Assert.Equal("Notch", player.Username);
    }

    [Fact]
    public async Task Resolve_NullUsernameInResponse_ParsesAsNull()
    {
        var (client, _) = Make(_ => StubHandler.Json(
            HttpStatusCode.OK,
            "{\"uuid\":\"069a79f4-44e9-4726-a5be-fca90e38aaf5\",\"username\":null}"));

        var player = await client.ResolveAsync(PlayerIdentifier.FromUuid("x"));
        Assert.Equal("069a79f4-44e9-4726-a5be-fca90e38aaf5", player.Uuid);
        Assert.Null(player.Username);
    }

    [Fact]
    public async Task Resolve_MalformedBody_ThrowsUnknown()
    {
        var (client, _) = Make(_ => StubHandler.Json(HttpStatusCode.OK, "not json"));
        var ex = await Assert.ThrowsAsync<SkinApiException>(
            () => client.ResolveAsync(PlayerIdentifier.FromUuid("x")));
        Assert.Equal(SkinApiErrorCode.Unknown, ex.Code);
        Assert.Equal(200, ex.Status);
    }

    [Fact]
    public async Task Resolve_JsonBodyMissingUuid_ThrowsUnknown()
    {
        var (client, _) = Make(_ => StubHandler.Json(HttpStatusCode.OK, "{\"nope\":true}"));
        var ex = await Assert.ThrowsAsync<SkinApiException>(
            () => client.ResolveAsync(PlayerIdentifier.FromUuid("x")));
        Assert.Equal(SkinApiErrorCode.Unknown, ex.Code);
        Assert.Equal(200, ex.Status);
    }

    [Fact]
    public async Task Resolve_SetsUserAgent()
    {
        var (client, handler) = Make(_ => StubHandler.Json(HttpStatusCode.OK, ResolvedNotchJson));
        await client.ResolveAsync(PlayerIdentifier.FromUuid("x"));
        Assert.Equal("createrington-skin-api", handler.Requests[0].UserAgent);
    }

    [Fact]
    public async Task Resolve_NullIdentifier_Throws()
    {
        var (client, _) = Make(_ => StubHandler.Json(HttpStatusCode.OK, ResolvedNotchJson));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.ResolveAsync(null!));
    }

    [Fact]
    public async Task Resolve_NotFound_MapsErrorCode()
    {
        var (client, _) = Make(
            _ => StubHandler.Json(HttpStatusCode.NotFound,
                "{\"error\":{\"code\":\"NOT_FOUND\",\"message\":\"Player missing\"}}"),
            retries: 0);

        var ex = await Assert.ThrowsAsync<SkinApiException>(
            () => client.ResolveAsync(PlayerIdentifier.FromUsername("ghost")));
        Assert.Equal(SkinApiErrorCode.NotFound, ex.Code);
        Assert.Equal(404, ex.Status);
        Assert.Equal("Player missing", ex.Message);
    }

    [Fact]
    public async Task Resolve_Retries429ThenSucceeds()
    {
        var (client, handler) = Make(
            i => i == 0
                ? StubHandler.Json((HttpStatusCode)429, "{\"error\":{\"code\":\"RATE_LIMITED\",\"retryAfterMs\":5}}")
                : StubHandler.Json(HttpStatusCode.OK, ResolvedNotchJson),
            retries: 1);

        var player = await client.ResolveAsync(PlayerIdentifier.FromUuid("x"));
        Assert.Equal("Notch", player.Username);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Resolve_CallerCancellation_PropagatesAndDoesNotSend()
    {
        var (client, handler) = Make(_ => StubHandler.Json(HttpStatusCode.OK, ResolvedNotchJson));
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.ResolveAsync(PlayerIdentifier.FromUuid("x"), cancellationToken: cts.Token));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public void PlayerIdentifier_Factories_ValidateInput()
    {
        Assert.Throws<ArgumentException>(() => PlayerIdentifier.FromUuid(""));
        Assert.Throws<ArgumentException>(() => PlayerIdentifier.FromUsername("  "));
        Assert.Throws<ArgumentNullException>(() => PlayerIdentifier.FromUuid(null!));
        Assert.Throws<ArgumentNullException>(() => PlayerIdentifier.FromUsername(null!));
    }

    [Fact]
    public void SkinSource_Factories_ValidateInput()
    {
        Assert.Throws<ArgumentException>(() => SkinSource.FromUuid(""));
        Assert.Throws<ArgumentException>(() => SkinSource.FromUsername("  "));
        Assert.Throws<ArgumentException>(() => SkinSource.FromPng(Array.Empty<byte>()));
        Assert.Throws<ArgumentNullException>(() => SkinSource.FromPng((byte[])null!));
    }

    [Fact]
    public async Task Render_NullSource_Throws()
    {
        var (client, _) = Make(_ => StubHandler.Png());
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.RenderAsync("wave", null!));
    }

    [Fact]
    public async Task Render_NormalizesUpperSnakeErrorCode()
    {
        var (client, _) = Make(
            _ => StubHandler.Json(HttpStatusCode.NotFound,
                "{\"error\":{\"code\":\"NOT_FOUND\",\"message\":\"Pose missing\"}}"),
            retries: 0);

        var ex = await Assert.ThrowsAsync<SkinApiException>(
            () => client.RenderAsync("wave", SkinSource.FromUuid("x")));
        Assert.Equal(SkinApiErrorCode.NotFound, ex.Code);
        Assert.Equal(404, ex.Status);
        Assert.Equal("Pose missing", ex.Message);
    }

    [Fact]
    public async Task Render_FallsBackToStatusCodeWhenBodyCodeUnknown()
    {
        var (client, _) = Make(
            _ => StubHandler.Json(HttpStatusCode.BadRequest, "{\"error\":{\"code\":\"WAT\",\"message\":\"huh\"}}"),
            retries: 0);

        var ex = await Assert.ThrowsAsync<SkinApiException>(
            () => client.RenderAsync("wave", SkinSource.FromUuid("x")));
        Assert.Equal(SkinApiErrorCode.BadRequest, ex.Code);
        Assert.Equal(400, ex.Status);
    }

    [Fact]
    public async Task Render_Retries429ThenSucceeds()
    {
        var (client, handler) = Make(
            i => i == 0
                ? StubHandler.Json((HttpStatusCode)429, "{\"error\":{\"code\":\"RATE_LIMITED\",\"retryAfterMs\":5}}")
                : StubHandler.Png(),
            retries: 1);

        var png = await client.RenderAsync("wave", SkinSource.FromUuid("x"));
        Assert.Equal(TestData.PngBytes, png);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Render_DoesNotRetryWhenRetriesZero()
    {
        var (client, handler) = Make(
            _ => StubHandler.Json((HttpStatusCode)429, "{\"error\":{\"code\":\"RATE_LIMITED\",\"retryAfterMs\":5}}"),
            retries: 0);

        var ex = await Assert.ThrowsAsync<SkinApiException>(
            () => client.RenderAsync("wave", SkinSource.FromUuid("x")));
        Assert.Equal(SkinApiErrorCode.RateLimited, ex.Code);
        Assert.Equal(429, ex.Status);
        Assert.Equal(5, ex.RetryAfterMs);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Render_NetworkError_MapsToNetworkErrorCode()
    {
        var (client, _) = Make(_ => throw new HttpRequestException("boom"), retries: 0);
        var ex = await Assert.ThrowsAsync<SkinApiException>(
            () => client.RenderAsync("wave", SkinSource.FromUuid("x")));
        Assert.Equal(SkinApiErrorCode.NetworkError, ex.Code);
        Assert.Equal(0, ex.Status);
    }

    [Fact]
    public async Task Render_Timeout_MapsToTimeoutCode()
    {
        var (client, _) = Make(_ => throw new TaskCanceledException(), retries: 0);
        var ex = await Assert.ThrowsAsync<SkinApiException>(
            () => client.RenderAsync("wave", SkinSource.FromUuid("x")));
        Assert.Equal(SkinApiErrorCode.Timeout, ex.Code);
        Assert.Equal(0, ex.Status);
    }

    [Fact]
    public async Task Render_CallerCancellation_PropagatesAndDoesNotSend()
    {
        var (client, handler) = Make(_ => StubHandler.Png());
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.RenderAsync("wave", SkinSource.FromUuid("x"), cancellationToken: cts.Token));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public void Constructor_RejectsNonPositiveTimeout()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SkinApiClient("k", new SkinApiClientOptions { Timeout = TimeSpan.Zero }));
    }

    [Fact]
    public void Constructor_RejectsNegativeRetries()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SkinApiClient("k", new SkinApiClientOptions { Retries = -1 }));
    }

    [Fact]
    public void Constructor_AllowsInfiniteTimeout()
    {
        using var client = new SkinApiClient(
            "k", new SkinApiClientOptions { Timeout = Timeout.InfiniteTimeSpan });
        Assert.NotNull(client);
    }

    [Fact]
    public void KnownPoses_Populated()
    {
        Assert.NotEmpty(KnownPoses.All);
        Assert.Contains("wave", KnownPoses.All);
        Assert.Equal("wave", KnownPoses.Wave);
    }

    [Fact]
    public void KnownPoses_Random_IsAlwaysKnown()
    {
        for (var i = 0; i < 50; i++)
        {
            Assert.Contains(KnownPoses.Random(), KnownPoses.All);
        }
    }
}
