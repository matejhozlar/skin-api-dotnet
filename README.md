<p align="center">
  <img src="https://raw.githubusercontent.com/matejhozlar/skin-api-dotnet/main/assets/og-card.png" alt="Createrington Skin API - .NET client" />
</p>

# Createrington.SkinApi

Official C# client for the Createrington Skin API. Renders Minecraft player
skins into named poses and returns PNG bytes. Targets .NET 8+, async, fully
documented.

```sh
dotnet add package Createrington.SkinApi
```

> Access is invite-only. Request an API key at https://api.createrington.com.

## Quickstart

```csharp
using Createrington.SkinApi;

using var client = new SkinApiClient("sk_..."); // or set SKIN_API_KEY

// Render a known pose for a Minecraft account by UUID.
byte[] png = await client.RenderAsync(
    "wave", SkinSource.FromUuid("069a79f444e94726a5befca90e38aaf5"));

await File.WriteAllBytesAsync("notch-waving.png", png);
```

Render options and other sources:

```csharp
byte[] png = await client.RenderAsync(
    KnownPoses.Cheer,
    SkinSource.FromUsername("Notch"),
    new RenderOptions { Slim = true, Outline = true, Width = 256, Height = 384 });

byte[] fromBytes = await client.RenderAsync("wave", SkinSource.FromPng(myPngBytes));
```

`Outline = true` draws a solid outline around the figure; it is off by default,
and leaving it unset sends nothing extra.

## Avatars

`AvatarAsync` returns a flat 2D front-view avatar: the head's face with the hat
layer composited on top, as a square PNG. It takes the same skin sources as
`RenderAsync` but has its own options (`Size`, `Overlay`); there is no pose.

```csharp
byte[] avatar = await client.AvatarAsync(
    SkinSource.FromUuid("069a79f444e94726a5befca90e38aaf5"),
    new AvatarOptions { Size = 128, Overlay = true });

await File.WriteAllBytesAsync("notch-avatar.png", avatar);
```

`Size` is the square edge length in pixels (8..512, default 64). `Overlay`
composites the hat layer over the face and is on by default; set it to `false`
to drop the hat. Both apply to every skin source: the `uuid`/`username` GET
path, the `skinUrl`/`skinBase64` JSON sources, and multipart uploads.

## Client

```csharp
// Self-managed HttpClient:
var client = new SkinApiClient(apiKey: "sk_...", options: new SkinApiClientOptions
{
    BaseUrl = new Uri("https://api.createrington.com"),
    Timeout = TimeSpan.FromSeconds(30),
    Retries = 2,            // retries 429/502/503/504 and network errors
    UserAgent = "createrington-skin-api",
});
```

`apiKey` falls back to the `SKIN_API_KEY` environment variable when null.

### IHttpClientFactory

Pass a managed `HttpClient` (e.g. from `IHttpClientFactory`) to the second
constructor. The client uses it as-is and does not dispose it or mutate its
default headers:

```csharp
public sealed class SkinService(IHttpClientFactory factory)
{
    public async Task<byte[]> WaveAsync(string uuid, CancellationToken ct)
    {
        var http = factory.CreateClient("skin-api");
        var client = new SkinApiClient(http, apiKey: "sk_...");
        return await client.RenderAsync("wave", SkinSource.FromUuid(uuid), cancellationToken: ct);
    }
}
```

## Skin sources

Exactly one source per call, created with a factory:

| Factory | Sent as |
| --- | --- |
| `SkinSource.FromUuid(uuid)` | JSON, resolved server-side |
| `SkinSource.FromUsername(name)` | JSON, resolved server-side |
| `SkinSource.FromUrl(url)` | JSON; public URL to a 64x64 PNG |
| `SkinSource.FromBase64(b64)` | JSON; base64 PNG (data URL prefix optional) |
| `SkinSource.FromPng(bytes)` | `multipart/form-data` upload |

`KnownPoses` exposes the poses bundled at build time (`KnownPoses.Wave`,
`KnownPoses.All`, ...), but `RenderAsync` accepts any pose string so
server-added poses work without an SDK upgrade. `KnownPoses.Random()` returns a
uniformly random pose name:

```csharp
byte[] png = await client.RenderAsync(KnownPoses.Random(), SkinSource.FromUuid(uuid));
```

## Errors

Non-success responses and network/timeout failures throw `SkinApiException`:

```csharp
try
{
    await client.RenderAsync("wave", SkinSource.FromUuid("bad-uuid"));
}
catch (SkinApiException ex)
{
    Console.Error.WriteLine($"{ex.Code} {ex.Status}: {ex.Message}");
    if (ex.Code == SkinApiErrorCode.RateLimited && ex.RetryAfterMs is int ms)
    {
        // back off and retry
    }
}
```

`ex.Status` is the HTTP status, or `0` for network/timeout failures. The client
retries `429`/`502`/`503`/`504` and network errors up to `Retries` times with
exponential backoff, honouring the server's `retryAfterMs` on `429` (capped).
Cancellation via the `CancellationToken` surfaces as `OperationCanceledException`,
not `SkinApiException`.

## Building

```sh
dotnet restore Createrington.SkinApi.sln
dotnet build Createrington.SkinApi.sln -c Release
dotnet test Createrington.SkinApi.sln -c Release
```

`KnownPoses` is generated from the published OpenAPI document (fetched live, not
committed):

```sh
dotnet run --project tools/GeneratePoses
```

## Contributing

Issues and pull requests are welcome. By submitting a contribution you agree it
is licensed under the project's Apache-2.0 terms (section 5 of the license); no
separate CLA is required.

## License

Apache-2.0. See [LICENSE](LICENSE).
