// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;

namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent]
public sealed partial class ModProjectileDampenerModuleComponent : Component
{
    [DataField]
    public float Radius = 1.5f;

    [DataField]
    public float SpeedMultiplier = 0.04f;

    [DataField]
    public float DamageMultiplier = 0.75f;

    [ViewVariables]
    public EntityUid? Wearer;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModDampenerFieldComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Module;

    [DataField, AutoNetworkedField]
    public float Radius;

    [DataField, AutoNetworkedField]
    public float SpeedMultiplier;

    [DataField, AutoNetworkedField]
    public float DamageMultiplier;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModDampenedProjectileComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Field;

    [DataField, AutoNetworkedField]
    public float OriginalSpeed;
}

[Serializable, NetSerializable]
public enum ModShootingMode : byte
{
    Off,
    Stormtrooper,
    Sharpshooter,
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(false)]
public sealed partial class ModShootingAssistantModuleComponent : Component
{
    [DataField, AutoNetworkedField]
    public ModShootingMode Mode;

    [DataField]
    public EntProtoId CycleAction = "ActionModShootingMode";

    [ViewVariables]
    public EntityUid? CycleActionEntity;

    [DataField]
    public float StormtrooperFireRate = 1.5f;

    [DataField]
    public float StormtrooperMinSpread = 15f;

    [DataField]
    public float StormtrooperMaxSpread = 25f;

    [DataField]
    public float SharpshooterSpreadMultiplier = 0.35f;

    [DataField]
    public float SharpshooterProjectileSpeed = 1.3f;

    [DataField]
    public float SharpshooterDamage = 1.15f;

    [DataField]
    public float SharpshooterMoveSpeed = 0.75f;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModShootingAssistantComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Module;
}

public sealed partial class ModShootingModeActionEvent : InstantActionEvent;
