using Robust.Shared.GameStates;

namespace Content.Arcane.Shared.Medical.Respiration;

/// <summary>
/// Identifies forced medical sleep whose last source releases the sleep it originally caused.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MedicalSleepStatusEffectComponent : Component;
