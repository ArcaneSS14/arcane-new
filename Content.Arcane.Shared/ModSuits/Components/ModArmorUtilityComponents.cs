// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Clothing.Components;
using Content.Shared.Item;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ModEnergyShieldModuleComponent : Component
{
    [DataField]
    public int MaxCharges = 1;

    [DataField, AutoNetworkedField]
    public int Charges = 1;

    [DataField]
    public TimeSpan RechargeDelay = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan ChargeIncrement = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextCharge;

    [DataField]
    public float BlockPower = 30f;

    [DataField]
    public SoundSpecifier BlockSound = new SoundPathSpecifier("/Audio/Weapons/block_metal1.ogg");
}

[RegisterComponent]
public sealed partial class ModPlateCompressionModuleComponent : Component
{
    [DataField]
    public ProtoId<ItemSizePrototype> Size = "Normal";

    public ProtoId<ItemSizePrototype>? OriginalSize;
}

[RegisterComponent]
public sealed partial class ModChameleonModuleComponent : Component
{
    [DataField]
    public bool BuiltIn;

    public ChameleonClothingComponent? Granted;
}
