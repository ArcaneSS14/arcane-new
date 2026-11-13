using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.Voodoo;

/// <summary>A server-owned body link; never exposes DNA or a target name to clients.</summary>
[RegisterComponent]
public sealed partial class VoodooComponent : Component
{
    [DataField]
    public bool BloodBinding;

    [DataField]
    public FixedPoint2 BloodCost = 1;

    [DataField]
    public HashSet<ProtoId<ReagentPrototype>> BloodReagents = ["Blood", "InsectBlood", "CopperBlood", "AmmoniaBlood", "SulfurBlood", "AlienBlood"];

    [DataField]
    public float Damage = 10f;

    [DataField]
    public float DestructionDamage = 200f;

    [DataField]
    public EntityUid? Target;

    [DataField]
    public bool Destroyed;
}

/// <summary>Backreferences allow body deletion to clear every linked doll.</summary>
[RegisterComponent]
public sealed partial class VoodooTargetComponent : Component
{
    [DataField]
    public HashSet<EntityUid> Dolls = new();
}
