using Content.Arcane.Shared.Cards;
using Robust.Shared.Audio;

namespace Content.Arcane.Server.Cards;

/// <summary>Hidden cards are records, not replicated children or per-face physical prototypes.</summary>
[RegisterComponent]
public sealed partial class PlayingCardsStorageComponent : Component
{
    [DataField] public List<ProtoId<CardFacePrototype>> Content = [];
    [DataField] public string BackState = "back_black";
    [DataField] public bool FaceDown = true;
    [DataField] public List<PlayingCardRecord> Cards = [];
    [DataField] public SoundSpecifier ShuffleSound = new SoundCollectionSpecifier("ArcaneCardFan");
    [DataField] public SoundSpecifier PickupSound = new SoundCollectionSpecifier("ArcaneCardSlide");
    [DataField] public SoundSpecifier PlaceSound = new SoundCollectionSpecifier("ArcaneCardShove");

    public int Revision;
    public EntityUid? Viewer;
    public NetUserId? ViewerId;
    public bool HasFaceComponents;
}

[DataDefinition]
public sealed partial class PlayingCardRecord
{
    [DataField(required: true)] public ProtoId<CardFacePrototype> Face;
    [DataField] public string BackState = "back_black";
    [DataField] public bool FaceDown = true;
}
