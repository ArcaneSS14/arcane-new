// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
// Arcane-Start
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
// Arcane-End

namespace Content.Goobstation.Shared.Xenobiology.Components;

/// <summary>
/// This is used for slime latching damage, this can be expanded in the future to allow for special breed dependent effects.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SlimeDamageOvertimeComponent : Component
{
    [DataField]
    public EntityUid? SourceEntityUid;

    // Arcane-Start
    [DataField]
    public FixedPoint2 SuctionUnits = 2.5;

    [DataField]
    public ProtoId<ReagentPrototype> ToxinReagent = "XenobioSlimeToxin";

    [DataField]
    public FixedPoint2 ToxinUnits = 0.15;
    // Arcane-End

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            { "Caustic", 2.5 }
        }
    };
}
