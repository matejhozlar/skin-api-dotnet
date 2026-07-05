namespace Createrington.SkinApi;

/// <summary>
/// Identifies the player to resolve. Construct one with a factory method;
/// exactly one identifier is represented per instance.
/// </summary>
public sealed class PlayerIdentifier
{
    internal string QueryField { get; }
    internal string Value { get; }

    private PlayerIdentifier(string queryField, string value)
    {
        QueryField = queryField;
        Value = value;
    }

    /// <summary>Identify the player by Mojang UUID, dashed or compact.</summary>
    /// <param name="uuid">The player's Mojang UUID.</param>
    public static PlayerIdentifier FromUuid(string uuid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uuid);
        return new PlayerIdentifier("uuid", uuid);
    }

    /// <summary>Identify the player by Minecraft username (case-insensitive).</summary>
    /// <param name="username">The player's Minecraft username.</param>
    public static PlayerIdentifier FromUsername(string username)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        return new PlayerIdentifier("username", username);
    }
}
