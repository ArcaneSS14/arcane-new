using System.Linq;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Pinpointer;
using Content.Shared.PowerCell;
using Robust.Server.GameObjects;
// Arcane-Start
using Content.Shared.Medical.SuitSensors;
using Robust.Shared.Prototypes;
// Arcane-End

namespace Content.Server.Medical.CrewMonitoring;

public sealed partial class CrewMonitoringConsoleSystem : EntitySystem
{
    [Dependency] private PowerCellSystem _cell = default!;
    [Dependency] private UserInterfaceSystem _uiSystem = default!;
    [Dependency] private IPrototypeManager _proto = default!; // Arcane

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);
    }

    private void OnRemove(EntityUid uid, CrewMonitoringConsoleComponent component, ComponentRemove args)
    {
        component.ConnectedSensors.Clear();
    }

    [SubscribeLocalEvent]
    private void OnSuitSensorBroadcast(Entity<CrewMonitoringConsoleComponent> ent, ref DeviceNetworkPacketEvent<BroadcastSuitSensorStatePayload> args)
    {
        ent.Comp.ConnectedSensors = args.Data.SensorStatus;
        UpdateUserInterface(ent, ent.Comp);
    }

    private void OnUIOpened(EntityUid uid, CrewMonitoringConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (!_cell.TryUseActivatableCharge(uid))
            return;

        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(EntityUid uid, CrewMonitoringConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!_uiSystem.IsUiOpen(uid, CrewMonitoringUIKey.Key))
            return;

        // The grid must have a NavMapComponent to visualize the map in the UI
        var xform = Transform(uid);

        if (xform.GridUid != null)
            EnsureComp<NavMapComponent>(xform.GridUid.Value);

        // Update all sensors info
        var allSensors = GetVisibleSensors(component).ToList(); // Arcane-Edit: ConnectedSensors.Values > GetVisibleSensors(component)
        _uiSystem.SetUiState(uid, CrewMonitoringUIKey.Key, new CrewMonitoringState(allSensors));
    }

    // Arcane-Start
    /// <summary>
    ///     Sensors this monitor may show, limited to its departments (Orion #326).
    ///     Department names are trimmed to the allowed ones so the UI groups people only under them.
    /// </summary>
    public IEnumerable<SuitSensorStatus> GetVisibleSensors(CrewMonitoringConsoleComponent component)
    {
        if (component.Departments.Count == 0)
            return component.ConnectedSensors.Values;

        var allowed = component.Departments.Select(d => Loc.GetString(_proto.Index(d).Name)).ToHashSet();
        return component.ConnectedSensors.Values
            .Where(s => s.JobDepartments.Any(allowed.Contains))
            .Select(s =>
            {
                s.JobDepartments = s.JobDepartments.Where(allowed.Contains).ToList();
                return s;
            });
    }
    // Arcane-End
}
