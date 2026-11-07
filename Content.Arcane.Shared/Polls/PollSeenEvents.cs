using Robust.Shared.Serialization;

namespace Content.Arcane.Shared.Polls;

[Serializable, NetSerializable]
public sealed class MarkPollSeenEvent(int pollId) : EntityEventArgs
{
    public int PollId { get; } = pollId;
}

[Serializable, NetSerializable]
public sealed class PollSeenAcknowledgedEvent(int pollId, bool seen) : EntityEventArgs
{
    public int PollId { get; } = pollId;
    public bool Seen { get; } = seen;
}
