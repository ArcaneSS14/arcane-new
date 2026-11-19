using Content.Shared.Atmos;

namespace Content.Arcane.Shared.Power;

[RegisterComponent]
public sealed partial class SupermatterMonitorComponent : Component
{
    public EntityUid? Focus;
}

[Serializable, NetSerializable]
public enum SupermatterMonitorUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SupermatterMonitorSelectMessage(NetEntity crystal) : BoundUserInterfaceMessage
{
    public NetEntity Crystal = crystal;
}

[Serializable, NetSerializable]
public sealed class SupermatterMonitorState(SupermatterMonitorEntry[] crystals, SupermatterMonitorReading? reading)
    : BoundUserInterfaceState
{
    public SupermatterMonitorEntry[] Crystals = crystals;
    public SupermatterMonitorReading? Reading = reading;
}

[Serializable, NetSerializable]
public readonly record struct SupermatterMonitorEntry(NetEntity Crystal, string Name, SupermatterMonitorStatus Status);

[Serializable, NetSerializable]
public readonly record struct SupermatterMonitorReading(
    NetEntity Crystal,
    float Integrity,
    float Power,
    float Radiation,
    float Moles,
    float Temperature,
    float TemperatureLimit,
    float Waste,
    float Absorption,
    SupermatterMonitorGas[] Gases);

[Serializable, NetSerializable]
public readonly record struct SupermatterMonitorGas(Gas Gas, float Proportion);

[Serializable, NetSerializable]
public enum SupermatterMonitorStatus : byte
{
    Inactive,
    Normal,
    Warning,
    Emergency,
    Delaminating,
}
