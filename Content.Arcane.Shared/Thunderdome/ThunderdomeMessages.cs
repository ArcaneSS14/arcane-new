using Content.Shared.Eui;

namespace Content.Arcane.Shared.Thunderdome;

[Serializable, NetSerializable]
public sealed class ThunderdomeLoadoutState(ThunderdomeLoadoutOption[] options) : EuiStateBase
{
    public ThunderdomeLoadoutOption[] Options { get; } = options;
}

[Serializable, NetSerializable]
public sealed record ThunderdomeLoadoutOption(string Name, string Description, string Category, EntProtoId Sprite);

[Serializable, NetSerializable]
public sealed class ThunderdomeSelectLoadoutMessage(int index) : EuiMessageBase
{
    public int Index { get; } = index;
}

[Serializable, NetSerializable]
public sealed class ThunderdomeRevivalOfferEvent : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class ThunderdomeRevivalAcceptEvent : EntityEventArgs;
