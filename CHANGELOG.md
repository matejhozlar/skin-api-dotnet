# Createrington.SkinApi (.NET)

This changelog tracks the Createrington Skin API C# SDK. A release publishes to
NuGet when a `<Version>` bump is merged to `main`.

## v2.5.0

### Added

- `AvatarAsync(source, options?, cancellationToken?)` returns a flat 2D
  front-view avatar (the head's face with the hat layer composited on top) as a
  square PNG. It takes the same skin sources as `RenderAsync` and uses the same
  GET/POST transport, auth, retries, and error handling. `AvatarOptions` exposes
  `Size` (8..512, default 64) and `Overlay` (default on). Additive and
  non-breaking.

## v2.4.0

### Changed

- `RenderAsync()` now calls the API over HTTP `GET` for `Uuid` and `Username`
  sources (the identifier rides in the query string), so these renders are plain
  cacheable URLs. `SkinUrl`, `SkinBase64`, and PNG uploads still use `POST`. The
  public API is unchanged and the server supports both, so this is non-breaking.

## v2.3.3

### Fixed

- Repository links in the package metadata now point to the public GitHub
  repository instead of the internal Gitea host.

## v2.3.2

### Changed

- Relicensed under Apache-2.0 (previously unlicensed). The public API is unchanged.
- The SDK now lives in its own open-source repository, and `KnownPoses` is
  generated from the published OpenAPI document rather than from server-side files.
- Version aligned with the other Createrington Skin API SDKs (Python, TypeScript)
  so all clients share one version line.

## v1.2.1

### Changed

- Boolean render params now serialize as `true`/`false` on the wire instead of
  `1`/`0` (`slim=true`, `outline=true`); `Outline` is still omitted when off. The
  public API is unchanged and the server accepts both forms, so this is
  non-breaking.

## v1.2.0

### Added

- `RenderOptions.Outline` requests a solid outline around the rendered figure.
  When true, the client sends `outline=1`; when false or unset it sends nothing,
  matching the server default (off). Additive and non-breaking.

## v1.1.0

### Added

- `KnownPoses.Random()` returns a uniformly random pose name. Additive and
  non-breaking.

## v1.0.0

Initial release on NuGet.

### Surface

- `SkinApiClient` with `RenderAsync(pose, source, options?, cancellationToken?)`
  returning `Task<byte[]>` (PNG bytes). Self-managed `HttpClient` or a
  caller-supplied one for `IHttpClientFactory` integration.
- `SkinSource` factories (`FromUuid`, `FromUsername`, `FromUrl`, `FromBase64`,
  `FromPng`) guaranteeing exactly one source at compile time.
- `apiKey` falls back to the `SKIN_API_KEY` environment variable; `BaseUrl`
  defaults to `https://api.createrington.com`.
- `KnownPoses` constants + `All` list generated from the server's pose data;
  `RenderAsync` still accepts any pose string.
- `SkinApiException` carrying `Code` (`SkinApiErrorCode`), `Status`, and
  `RetryAfterMs`. Server `UPPER_SNAKE` codes are normalized.
- Retries `429`/`502`/`503`/`504` and network errors with exponential backoff,
  honouring `retryAfterMs` on `429` (capped at 60s). Timeouts surface as
  `SkinApiErrorCode.Timeout`; caller cancellation as `OperationCanceledException`.
- XML doc comments on the public surface; zero third-party dependencies.
