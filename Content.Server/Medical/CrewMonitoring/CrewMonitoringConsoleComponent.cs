using Content.Shared.Medical.SuitSensors;
using Content.Shared.Roles; // Arcane
using Robust.Shared.Prototypes; // Arcane

namespace Content.Server.Medical.CrewMonitoring;

[RegisterComponent]
[Access(typeof(CrewMonitoringConsoleSystem))]
public sealed partial class CrewMonitoringConsoleComponent : Component
{
    /// <summary>
    ///     List of all currently connected sensors to this console.
    /// </summary>
    public Dictionary<string, SuitSensorStatus> ConnectedSensors = new();

    /// <summary>
    ///     After what time sensor consider to be lost.
    /// </summary>
    [DataField("sensorTimeout"), ViewVariables(VVAccess.ReadWrite)]
    public float SensorTimeout = 10f;

    // Arcane-Start
    /// <summary>
    ///     Departments this monitor shows, all of them when empty (Orion #326).
    /// </summary>
    [DataField]
    public List<ProtoId<DepartmentPrototype>> Departments = new();
    // Arcane-End
}
