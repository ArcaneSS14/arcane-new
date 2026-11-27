namespace Content.Arcane.Common.Administration;

[Serializable, NetSerializable]
public readonly record struct AdminLogCursor(DateTime Date, int RoundId, int Id) : IComparable<AdminLogCursor>
{
    public int CompareTo(AdminLogCursor other)
    {
        var dateOrder = Date.CompareTo(other.Date);
        if (dateOrder != 0)
            return dateOrder;

        var roundOrder = RoundId.CompareTo(other.RoundId);
        return roundOrder != 0 ? roundOrder : Id.CompareTo(other.Id);
    }
}

[Serializable, NetSerializable]
public sealed class BwoinkHistoryRequest(NetUserId channel, AdminLogCursor? cursor = null) : EntityEventArgs
{
    public NetUserId Channel { get; } = channel;
    public AdminLogCursor? Cursor { get; } = cursor;
}

[Serializable, NetSerializable]
public sealed class BwoinkHistoryResponse(
    NetUserId channel,
    List<BwoinkHistoryMessage> messages,
    AdminLogCursor? nextCursor,
    bool hasMore,
    AdminLogCursor? requestCursor) : EntityEventArgs
{
    public NetUserId Channel { get; } = channel;
    public List<BwoinkHistoryMessage> Messages { get; } = messages;
    public AdminLogCursor? NextCursor { get; } = nextCursor;
    public bool HasMore { get; } = hasMore;
    public AdminLogCursor? RequestCursor { get; } = requestCursor;
}

[Serializable, NetSerializable]
public sealed record BwoinkHistoryMessage(DateTime SentAt, string Text, bool AdminOnly, int RoundId);

public sealed class BwoinkHistoryEntryEvent(NetUserId channel, string text, bool adminOnly) : EntityEventArgs
{
    public NetUserId Channel { get; } = channel;
    public string Text { get; } = text;
    public bool AdminOnly { get; } = adminOnly;
}
