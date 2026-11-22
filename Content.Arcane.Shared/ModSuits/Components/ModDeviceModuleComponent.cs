using Content.Arcane.Shared.ModSuits.Systems;
using Content.Trauma.Common.RadialSelector;

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

[RegisterComponent, Access(typeof(ModSuitSystem))]
public sealed partial class ModDeviceSelectorComponent : Component
{
    [DataField(required: true)]
    public List<RadialSelectorEntry> Entries = [];

    [DataField]
    public EntProtoId Action = "ActionModSelectDevice";

    public EntityUid? ActionEntity;
}
