using System.Text.Json;

namespace Createrington.SkinApi;

/// <summary>A resolved player identity.</summary>
public sealed class ResolvedPlayer
{
    /// <summary>The player's UUID in canonical dashed lowercase form.</summary>
    public string Uuid { get; }

    /// <summary>
    /// The current username with canonical casing, or <see langword="null"/> when
    /// a degraded fallback provider could not supply the name.
    /// </summary>
    public string? Username { get; }

    /// <summary>Creates a new <see cref="ResolvedPlayer"/>.</summary>
    /// <param name="uuid">The player's UUID in canonical dashed lowercase form.</param>
    /// <param name="username">The current username, or null when unavailable.</param>
    public ResolvedPlayer(string uuid, string? username)
    {
        Uuid = uuid;
        Username = username;
    }

    internal static ResolvedPlayer FromJson(byte[] body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("uuid", out var uuid)
                && uuid.ValueKind == JsonValueKind.String)
            {
                string? username = null;
                if (root.TryGetProperty("username", out var name)
                    && name.ValueKind == JsonValueKind.String)
                {
                    username = name.GetString();
                }

                return new ResolvedPlayer(uuid.GetString()!, username);
            }
        }
        catch (JsonException)
        {
        }

        throw new SkinApiException(
            "The server returned a malformed resolve response.",
            SkinApiErrorCode.Unknown,
            200);
    }
}
