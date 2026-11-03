namespace Content.Arcane.Common.Gravity;

/// <summary>
/// Raised broadcast when a grid's gravity is recalculated and no gravity generator powers it, so other
/// gravity providers on <see cref="Grid"/> can keep it on by setting <see cref="Enabled"/>.
/// </summary>
[ByRefEvent]
public record struct GravityProviderQueryEvent(EntityUid Grid, bool Enabled = false);
