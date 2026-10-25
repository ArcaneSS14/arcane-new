using Content.Shared.Damage;

namespace Content.Arcane.Common.Medical.Respiration;

/// <summary>
/// Allows respiratory balance to change damage without replacing breathing or gas exchange.
/// </summary>
[ByRefEvent]
public struct RespirationDamageEvent(DamageSpecifier damage, bool suffocating, TimeSpan interval)
{
    public DamageSpecifier Damage = damage;
    public readonly bool Suffocating = suffocating;
    public readonly TimeSpan Interval = interval;
}
