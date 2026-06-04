namespace Createrington.SkinApi;

/// <summary>Optional render parameters. Unset values fall back to server defaults.</summary>
public sealed class RenderOptions
{
    /// <summary>
    /// Override slim ("Alex") arm geometry. When null, the server uses the
    /// skin's own model metadata.
    /// </summary>
    public bool? Slim { get; set; }

    /// <summary>
    /// Draw a solid outline around the rendered figure. When null or false, no
    /// outline is requested and the server default (off) applies.
    /// </summary>
    public bool? Outline { get; set; }

    /// <summary>Output width in pixels (64..2048). Defaults to 400 server-side.</summary>
    public int? Width { get; set; }

    /// <summary>Output height in pixels (64..2048). Defaults to 600 server-side.</summary>
    public int? Height { get; set; }
}
