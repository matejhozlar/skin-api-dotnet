# Contributing

Thanks for your interest in improving the Createrington Skin API .NET client.
Issues and pull requests are welcome.

## Licensing of contributions

By submitting a pull request you agree that your contribution is licensed under
the project's [Apache-2.0](LICENSE) terms (per section 5 of the license). There
is no separate CLA to sign.

## Prerequisites

- .NET 8 SDK or newer.

```sh
dotnet restore Createrington.SkinApi.sln
dotnet build Createrington.SkinApi.sln -c Release
dotnet test Createrington.SkinApi.sln -c Release
```

The build treats warnings as errors and `Nullable` is enabled, so a green local
build is the bar a PR has to clear.

## Project layout

- `src/Createrington.SkinApi/` is the published library.
- `tests/Createrington.SkinApi.Tests/` holds the xUnit tests (they run against a
  stubbed `HttpClient`, so they need no network or API key).
- `tools/GeneratePoses/` regenerates `KnownPoses.cs`.

`src/Createrington.SkinApi/KnownPoses.cs` is **generated, not hand-edited**. It is
produced from the published OpenAPI document. If you need to refresh it:

```sh
dotnet run --project tools/GeneratePoses
```

Note that `RenderAsync` accepts any pose string, so a new server-side pose works
without changing the SDK; `KnownPoses` only provides compile-time names.

## Branching and pull requests

- Branch off `dev`, and open your PR against `dev`. `main` is the released
  branch; merges to it publish to NuGet.
- Use short, descriptive branch names like `feat/retry-jitter`,
  `fix/timeout-mapping`, `chore/bump-xunit`.
- Keep a PR focused on one change, and make sure the build and tests pass.

## Commit messages

Use Conventional Commit style:

```
type(scope): description
```

- Types: `feat`, `fix`, `chore`, `refactor`, `docs`, `style`, `test`, `perf`.
- Scope is optional.
- Description is lowercase, imperative, and has no trailing period.

Examples:

```
feat: add cancellation support to RenderAsync
fix: map 503 to UpstreamUnavailable
docs: document the IHttpClientFactory constructor
```

## Code style

- Public types and members carry XML doc comments.
- Default to no comments; add one only when the reasoning is not obvious from the
  code itself.
- Avoid em dashes in code, comments, and docs; use commas, parentheses, colons,
  or hyphens.

## Reporting issues

Open an issue with a clear description and, where relevant, a minimal repro (the
pose, the skin source, the client options, and the observed vs expected result).
