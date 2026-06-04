namespace Createrington.SkinApi;

/// <summary>Configuration for a <see cref="SkinApiClient"/>.</summary>
public sealed class SkinApiClientOptions
{
    /// <summary>The API base URL. Defaults to <c>https://api.createrington.com</c>.</summary>
    public Uri BaseUrl { get; set; } = new Uri("https://api.createrington.com");

    /// <summary>Per-request timeout. Defaults to 30 seconds.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Number of retries for <c>429</c>/<c>502</c>/<c>503</c>/<c>504</c> responses
    /// and network errors, with exponential backoff. Defaults to 2.
    /// </summary>
    public int Retries { get; set; } = 2;

    /// <summary>The User-Agent header sent with each request.</summary>
    public string UserAgent { get; set; } = "createrington-skin-api";
}
