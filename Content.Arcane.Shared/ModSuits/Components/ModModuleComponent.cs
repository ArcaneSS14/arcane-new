using Content.Arcane.Shared.ModSuits.Systems;
using Content.Shared.Whitelist;

namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(ModSuitSystem))]
public sealed partial class ModModuleComponent : Component
{
    [DataField("moduleType")]
    public ModModuleType Type;

    [DataField]
    public int Complexity;

    [DataField]
    public float IdlePower;

    [DataField]
    public float ActivePower;

    [DataField]
    public float UsePower;

    [DataField]
    public TimeSpan Cooldown;

    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUse;

    [DataField, AutoNetworkedField]
    public bool Removable = true;

    [DataField]
    public EntityWhitelist? Incompatible;

    [DataField]
    public List<string> RequiredParts = [];

    [DataField]
    public ModModuleAllowFlags AllowFlags;

    [DataField]
    public EntProtoId? Action = "ActionModModule";

    public EntityUid? ActionEntity;

    [AutoNetworkedField]
    public bool Active;

    [DataField, AutoNetworkedField]
    public bool Pinned = true;

    [AutoNetworkedField]
    public EntityUid? Suit;
}

public enum ModModuleType : byte
{
    Passive,
    Usable,
    Toggle,
    Active,
}

[Flags]
public enum ModModuleAllowFlags : byte
{
    None = 0,
    Inactive = 1 << 0,
    Unworn = 1 << 1,
    Incapacitated = 1 << 2,
}
