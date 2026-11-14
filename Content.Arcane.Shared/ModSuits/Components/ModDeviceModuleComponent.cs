using Content.Arcane.Shared.ModSuits.Systems;

namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent, Access(typeof(ModSuitSystem))]
public sealed partial class ModDeviceModuleComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Device;

    public EntityUid? DeviceEntity;
}

[RegisterComponent, Access(typeof(ModSuitSystem))]
public sealed partial class ModDeviceItemComponent : Component
{
    public EntityUid Module;
}
