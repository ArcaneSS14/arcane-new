using Content.Shared.Damage;

namespace Content.Shared.Armor;

/// <summary>
/// Modifiers for this armor after temporary effects. Handlers must copy the baseline before editing it.
/// </summary>
[ByRefEvent]
public record struct ArmorModifiersQueryEvent(DamageModifierSet Modifiers);
