using Content.Shared.Damage;

namespace Content.Arcane.Common.Medical.Wounds;

/// <summary>
/// Applies a medical item's healing and reports the budget consumed by raw damage and wounds together.
/// </summary>
[ByRefEvent]
public struct MedicalHealingEvent(DamageSpecifier damage, EntityUid user, bool ignoreBlockers)
{
    public readonly DamageSpecifier Damage = damage;
    public readonly EntityUid User = user;
    public readonly bool IgnoreBlockers = ignoreBlockers;
    public DamageSpecifier? Consumed;
}
