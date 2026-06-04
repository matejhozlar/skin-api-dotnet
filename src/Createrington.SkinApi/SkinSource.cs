using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Createrington.SkinApi;

/// <summary>
/// Identifies the Minecraft skin to render. Construct one with a factory
/// method; exactly one source is represented per instance.
/// </summary>
public sealed class SkinSource
{
    internal enum SourceKind
    {
        Uuid,
        Username,
        SkinUrl,
        SkinBase64,
        Png,
    }

    internal SourceKind Kind { get; }
    internal string? Value { get; }
    internal byte[]? Png { get; }

    private SkinSource(SourceKind kind, string? value, byte[]? png)
    {
        Kind = kind;
        Value = value;
        Png = png;
    }

    /// <summary>Resolve the skin server-side from a Mojang UUID.</summary>
    /// <param name="uuid">The player's Mojang UUID.</param>
    public static SkinSource FromUuid(string uuid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uuid);
        return new SkinSource(SourceKind.Uuid, uuid, null);
    }

    /// <summary>Resolve the skin server-side from a Mojang username.</summary>
    /// <param name="username">The player's Mojang username.</param>
    public static SkinSource FromUsername(string username)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        return new SkinSource(SourceKind.Username, username, null);
    }

    /// <summary>Fetch the skin from a publicly reachable URL to a 64x64 PNG.</summary>
    /// <param name="skinUrl">A public URL to a 64x64 PNG skin.</param>
    public static SkinSource FromUrl(string skinUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skinUrl);
        return new SkinSource(SourceKind.SkinUrl, skinUrl, null);
    }

    /// <summary>Use a base64-encoded 64x64 PNG (with or without a data URL prefix).</summary>
    /// <param name="skinBase64">The base64-encoded PNG.</param>
    public static SkinSource FromBase64(string skinBase64)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skinBase64);
        return new SkinSource(SourceKind.SkinBase64, skinBase64, null);
    }

    /// <summary>Upload raw 64x64 PNG bytes as multipart form data.</summary>
    /// <param name="png">The raw PNG bytes.</param>
    public static SkinSource FromPng(byte[] png)
    {
        ArgumentNullException.ThrowIfNull(png);
        if (png.Length == 0)
        {
            throw new ArgumentException("PNG bytes must not be empty.", nameof(png));
        }

        return new SkinSource(SourceKind.Png, null, png);
    }

    /// <summary>Upload raw 64x64 PNG bytes as multipart form data.</summary>
    /// <param name="png">The raw PNG bytes.</param>
    public static SkinSource FromPng(ReadOnlySpan<byte> png) => FromPng(png.ToArray());

    internal HttpContent CreateContent()
    {
        if (Kind == SourceKind.Png)
        {
            var form = new MultipartFormDataContent();
            var part = new ByteArrayContent(Png!);
            part.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(part, "skin", "skin.png");
            return form;
        }

        var field = Kind switch
        {
            SourceKind.Uuid => "uuid",
            SourceKind.Username => "username",
            SourceKind.SkinUrl => "skinUrl",
            SourceKind.SkinBase64 => "skinBase64",
            _ => throw new InvalidOperationException($"Unhandled source kind: {Kind}"),
        };

        var json = JsonSerializer.Serialize(new Dictionary<string, string> { [field] = Value! });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
