namespace Content.Arcane.Shared.Cards;

/// <summary>Only the count and publicly visible card surfaces cross the entity-state boundary.</summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class PlayingCardsComponent : Component
{
    public const string SpritePath = "/Textures/_Arcane/Objects/Misc/cards.rsi";

    [DataField, AutoNetworkedField]
    public int Count;

    [DataField, AutoNetworkedField]
    public PlayingCardKind Kind = PlayingCardKind.Single;

    [DataField, AutoNetworkedField]
    public List<string> VisibleStates = [];
}

[Serializable, NetSerializable]
public enum PlayingCardKind : byte { Single, Hand, Deck }

/// <summary>Used by the existing deck-box whitelist and counter.</summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PlayingCardDeckComponent : Component;

/// <summary>Catalog data does not identify which face is held by a physical, generic card.</summary>
[Prototype]
public sealed partial class CardFacePrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;
    [DataField(required: true)] public LocId Name;
    [DataField(required: true)] public string State = default!;
    [DataField] public ComponentRegistry Components = new();
}

[Serializable, NetSerializable]
public enum PlayingCardsUiKey : byte { Key }

[Serializable, NetSerializable]
public sealed class PlayingCardsRefreshMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class PlayingCardsDrawMessage(int revision, int index) : BoundUserInterfaceMessage
{
    public int Revision = revision;
    public int Index = index;
}

/// <summary>Sent to the current holder only, never through public BUI state.</summary>
[Serializable, NetSerializable]
public sealed class PlayingCardsSnapshotMessage(int revision, List<PlayingCardFace> cards) : BoundUserInterfaceMessage
{
    public int Revision = revision;
    public List<PlayingCardFace> Cards = cards;
}

[Serializable, NetSerializable]
public readonly record struct PlayingCardFace(string State, string Name);
