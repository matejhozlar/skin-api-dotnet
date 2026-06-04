namespace Createrington.SkinApi;

/// <summary>
/// Categorises a <see cref="SkinApiException"/>. Mirrors the error codes
/// emitted by the Skin API, plus client-side transport categories.
/// </summary>
public enum SkinApiErrorCode
{
    /// <summary>The error could not be mapped to a known code.</summary>
    Unknown = 0,

    /// <summary>The request was rejected as malformed (HTTP 400).</summary>
    BadRequest,

    /// <summary>The API key was missing or invalid (HTTP 401).</summary>
    Unauthorized,

    /// <summary>The API key is not permitted to perform the request (HTTP 403).</summary>
    Forbidden,

    /// <summary>The pose or resource was not found (HTTP 404).</summary>
    NotFound,

    /// <summary>The request conflicted with server state (HTTP 409).</summary>
    Conflict,

    /// <summary>The request body media type is not supported (HTTP 415).</summary>
    UnsupportedMediaType,

    /// <summary>The caller has exceeded a rate or volume quota (HTTP 429).</summary>
    RateLimited,

    /// <summary>The server encountered an internal error (HTTP 500).</summary>
    Internal,

    /// <summary>The render pipeline failed to produce an image.</summary>
    RenderFailed,

    /// <summary>An upstream dependency was unavailable (HTTP 502/503/504).</summary>
    UpstreamUnavailable,

    /// <summary>The request exceeded the configured client timeout.</summary>
    Timeout,

    /// <summary>A network-level failure occurred before a response was received.</summary>
    NetworkError,
}
