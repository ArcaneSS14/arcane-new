using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Shared.Gambling;

/// <summary>A weighted gambling outcome. An empty table represents a loss.</summary>
[Prototype]
public sealed partial class PrizePrototype : IPrototype
{
    [IdDataField] public string ID { get; set; } = default!;
    [DataField(required: true)] public float Weight;
    [DataField] public EntityTableSelector? PrizeTable;
    [DataField] public LocId? WinMessage;
    [DataField] public PrizeAnnounceType AnnounceType = PrizeAnnounceType.Speak;
    [DataField] public SoundSpecifier? WinSound;
}

public enum PrizeAnnounceType : byte
{
    Speak,
    Popup,
}
