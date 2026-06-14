namespace Createrington.SkinApi;

/// <summary>Optional avatar parameters. Unset values fall back to server defaults.</summary>
public sealed class AvatarOptions
{
    /// <summary>
    /// Output edge length in pixels (8..512). The image is square. Defaults to
    /// 64 server-side.
    /// </summary>
    public int? Size { get; set; }

    /// <summary>
    /// Composite the hat layer over the face. When null, the server default (on)
    /// applies.
    /// </summary>
    public bool? Overlay { get; set; }
}
