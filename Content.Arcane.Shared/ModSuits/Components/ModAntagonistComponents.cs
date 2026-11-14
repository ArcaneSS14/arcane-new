// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent]
public sealed partial class ModCloakModuleComponent : Component;

[RegisterComponent]
public sealed partial class ModCloakedComponent : Component
{
    public EntityUid Module;
}

[RegisterComponent]
public sealed partial class ModAdrenalineModuleComponent : Component
{
    [DataField(required: true)]
    public Solution Solution = new();

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");
}

[RegisterComponent]
public sealed partial class ModPowerKickModuleComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan ChargeTime = TimeSpan.FromSeconds(1);

    [DataField]
    public float Speed = 12f;

    [DataField]
    public float Range = 7f;

    [DataField]
    public SoundSpecifier ChargeSound = new SoundPathSpecifier("/Audio/Mecha/sound_mecha_hydraulic.ogg");

    [DataField]
    public SoundSpecifier HitSound = new SoundPathSpecifier("/Audio/Weapons/punch3.ogg");
}

[RegisterComponent]
public sealed partial class ModPowerKickingComponent : Component
{
    public EntityUid Module;
}
