using Content.Shared.Actions;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Arcane.Shared.Silicons.Boris;

public sealed partial class OpenBorisDevicesEvent : InstantActionEvent;
public sealed partial class ReturnFromBorisEvent : InstantActionEvent;

[Serializable, NetSerializable]
public enum BorisUiKey : byte
{
    Devices,
}

[Serializable, NetSerializable]
public enum BorisDeviceAction : byte
{
    View,
    Control,
}

[Serializable, NetSerializable]
public sealed class BorisDeviceMessage(NetEntity target, BorisDeviceAction action) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
    public readonly BorisDeviceAction Action = action;
}

[Serializable, NetSerializable]
public sealed class BorisDeviceState(List<BorisDevice> devices) : BoundUserInterfaceState
{
    public readonly List<BorisDevice> Devices = devices;
}

[Serializable, NetSerializable]
public readonly record struct BorisDevice(NetEntity Target, string Name);
