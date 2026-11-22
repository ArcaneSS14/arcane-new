using Content.Shared.Mobs;

namespace Content.Arcane.Shared.ModSuits;

[Serializable, NetSerializable]
public enum ModStatusReadoutUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class ModStatusReadoutState(
    string name,
    MobState state,
    float damage,
    float? temperature,
    float? blood,
    float? staminaDamage,
    string? fingerprint) : BoundUserInterfaceState
{
    public readonly string Name = name;
    public readonly MobState State = state;
    public readonly float Damage = damage;
    public readonly float? Temperature = temperature;
    public readonly float? Blood = blood;
    public readonly float? StaminaDamage = staminaDamage;
    public readonly string? Fingerprint = fingerprint;
}
