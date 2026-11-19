using Content.Shared.Roles;
using Robust.Shared.Map;

namespace Content.Arcane.Server.Thunderdome;

[RegisterComponent, Access(typeof(ThunderdomeSystem))]
public sealed partial class ThunderdomeRuleComponent : Component
{
    [DataField(required: true)]
    public ProtoId<StartingGearPrototype> Gear;

    [DataField(required: true)]
    public List<ThunderdomeLoadout> WeaponLoadouts = new();

    public MapId? Map;
    public HashSet<EntityUid> Grids = new();
    public TimeSpan NextSweep;
}

[DataDefinition]
public sealed partial class ThunderdomeLoadout
{
    [DataField(required: true)]
    public ProtoId<StartingGearPrototype> Gear;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public LocId Description;

    [DataField(required: true)]
    public LocId Category;

    [DataField(required: true)]
    public EntProtoId Sprite;
}
