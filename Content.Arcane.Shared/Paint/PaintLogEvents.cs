using Robust.Shared.Serialization;

namespace Content.Arcane.Shared.Paint;

[Serializable, NetSerializable]
public sealed class PaintLogImageRequestEvent(int round, string token) : EntityEventArgs
{
    public readonly int Round = round;
    public readonly string Token = token;
}

[Serializable, NetSerializable]
public sealed class PaintLogImageResponseEvent(string token, PaintImage? image) : EntityEventArgs
{
    public readonly string Token = token;
    public readonly PaintImage? Image = image;
}

public static class PaintLogToken
{
    public const string Prefix = "pnt";
    public const int HexLength = 12;

    public static string New()
    {
        return Prefix + Guid.NewGuid().ToString("N")[..HexLength];
    }

    public static bool TryFind(string message, out string token)
    {
        token = string.Empty;
        const string marker = " (painting ";
        if (!message.EndsWith(')'))
            return false;

        var start = message.LastIndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
            return false;

        start += marker.Length;
        var candidate = message.Substring(start, message.Length - start - 1);
        if (!IsValid(candidate))
            return false;

        token = candidate;
        return true;
    }

    public static bool IsValid(string token)
    {
        return token != null && token.Length == Prefix.Length + HexLength
               && token.StartsWith(Prefix, StringComparison.Ordinal)
               && IsHex(token, Prefix.Length, HexLength);
    }

    private static bool IsHex(string text, int start, int length)
    {
        for (var i = start; i < start + length; i++)
        {
            if (!Uri.IsHexDigit(text[i]))
                return false;
        }

        return true;
    }
}
