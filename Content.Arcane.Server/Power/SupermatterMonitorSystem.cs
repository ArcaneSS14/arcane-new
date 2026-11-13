using Content.Arcane.Shared.Power;
using Content.Goobstation.Shared.Supermatter.Components;
using Content.Shared.Atmos;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Radiation.Components;
using Robust.Server.GameObjects;

namespace Content.Arcane.Server.Power;

public sealed partial class SupermatterMonitorSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    private readonly HashSet<EntityUid> _openMonitors = [];
    private readonly Dictionary<EntityUid, List<SupermatterMonitorEntry>> _entriesByGrid = [];
    private float _untilRefresh;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SupermatterMonitorComponent, BoundUserInterfaceMessageAttempt>(OnUiAttempt);
        SubscribeLocalEvent<SupermatterMonitorComponent, BoundUIOpenedEvent>(OnOpen);
        SubscribeLocalEvent<SupermatterMonitorComponent, BoundUIClosedEvent>(OnClose);
        SubscribeLocalEvent<SupermatterMonitorComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SupermatterMonitorComponent, SupermatterMonitorSelectMessage>(OnSelect);
    }

    private void OnOpen(Entity<SupermatterMonitorComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!args.UiKey.Equals(SupermatterMonitorUiKey.Key))
            return;
        _openMonitors.Add(ent);
        Refresh(ent);
    }

    private void OnUiAttempt(Entity<SupermatterMonitorComponent> ent, ref BoundUserInterfaceMessageAttempt args)
    {
        if (!args.UiKey.Equals(SupermatterMonitorUiKey.Key))
            return;
        if (_hands.GetActiveItem(args.Actor) != ent.Owner ||
            _ui.IsUiOpen(ent.Owner, SupermatterMonitorUiKey.Key) &&
            !_ui.IsUiOpen(ent.Owner, SupermatterMonitorUiKey.Key, args.Actor))
            args.Cancel();
    }

    private void OnClose(Entity<SupermatterMonitorComponent> ent, ref BoundUIClosedEvent args)
    {
        if (args.UiKey.Equals(SupermatterMonitorUiKey.Key) && !_ui.IsUiOpen(ent.Owner, SupermatterMonitorUiKey.Key))
            _openMonitors.Remove(ent);
    }

    private void OnShutdown(Entity<SupermatterMonitorComponent> ent, ref ComponentShutdown args)
        => _openMonitors.Remove(ent);

    private void OnSelect(Entity<SupermatterMonitorComponent> ent, ref SupermatterMonitorSelectMessage args)
    {
        if (!args.UiKey.Equals(SupermatterMonitorUiKey.Key) ||
            !_ui.IsUiOpen(ent.Owner, SupermatterMonitorUiKey.Key, args.Actor) ||
            _hands.GetActiveItem(args.Actor) != ent.Owner ||
            !TryGetEntity(args.Crystal, out var crystal) || crystal is not { Valid: true } target ||
            TerminatingOrDeleted(target) || EntityManager.IsQueuedForDeletion(target) ||
            !HasComp<SupermatterComponent>(target) || Transform(ent).GridUid is not { Valid: true } grid ||
            Transform(target) is not { Anchored: true } targetTransform || targetTransform.GridUid != grid)
            return;

        ent.Comp.Focus = target;
        Refresh(ent);
    }

    public override void Update(float frameTime)
    {
        if (_openMonitors.Count == 0)
            return;
        _untilRefresh -= frameTime;
        if (_untilRefresh > 0f)
            return;
        _untilRefresh = 1f;

        foreach (var uid in _openMonitors)
        {
            if (TryComp<SupermatterMonitorComponent>(uid, out var monitor))
                Refresh((uid, monitor), shareEntries: true);
        }
        _entriesByGrid.Clear();
    }

    private void Refresh(Entity<SupermatterMonitorComponent> ent, bool shareEntries = false)
    {
        if (Transform(ent).GridUid is not { Valid: true } grid)
        {
            ent.Comp.Focus = null;
            _ui.SetUiState(ent.Owner, SupermatterMonitorUiKey.Key, new SupermatterMonitorState([], null));
            return;
        }

        var entries = GetEntries(grid, shareEntries);
        SupermatterMonitorReading? reading = null;
        foreach (var entry in entries)
        {
            if (TryGetEntity(entry.Crystal, out var crystal) && crystal == ent.Comp.Focus && crystal is { } uid)
            {
                reading = GetReading(uid, entry.Crystal);
                break;
            }
        }
        if (reading == null)
        {
            ent.Comp.Focus = null;
            if (entries.Count > 0 && TryGetEntity(entries[0].Crystal, out var first) && first is { } uid)
            {
                ent.Comp.Focus = uid;
                reading = GetReading(uid, entries[0].Crystal);
            }
        }

        _ui.SetUiState(ent.Owner, SupermatterMonitorUiKey.Key, new SupermatterMonitorState(entries.ToArray(), reading));
    }

    private List<SupermatterMonitorEntry> GetEntries(EntityUid grid, bool shareEntries)
    {
        if (shareEntries && _entriesByGrid.TryGetValue(grid, out var shared))
            return shared;
        var entries = new List<SupermatterMonitorEntry>();
        var query = EntityQueryEnumerator<SupermatterComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var sm, out var xform))
        {
            if (!xform.Anchored || xform.GridUid != grid || TerminatingOrDeleted(uid) ||
                EntityManager.IsQueuedForDeletion(uid))
                continue;
            entries.Add(new SupermatterMonitorEntry(GetNetEntity(uid), Name(uid), GetStatus(sm)));
        }
        if (shareEntries)
            _entriesByGrid[grid] = entries;
        return entries;
    }

    private SupermatterMonitorReading GetReading(EntityUid uid, NetEntity crystal)
    {
        var sm = Comp<SupermatterComponent>(uid);
        TryComp<SupermatterEventsComponent>(uid, out var sample);
        TryComp<RadiationSourceComponent>(uid, out var radiation);
        var gases = new List<SupermatterMonitorGas>();
        if (sample is { SampleMoles: > 0 })
        {
            for (var index = 0; index < sample.GasSample.Length; index++)
            {
                var amount = sample.GasSample[index];
                if (amount > 0f)
                    gases.Add(new SupermatterMonitorGas((Gas) index, amount / sample.SampleMoles));
            }
        }
        return new SupermatterMonitorReading(
            crystal,
            sm.DelaminationPoint > 0 ? Math.Clamp(100f - 100f * sm.Damage / sm.DelaminationPoint, 0f, 100f) : 0f,
            sm.Power, radiation?.EffectiveIntensity ?? 0f, sample?.SampleMoles ?? 0f, sample?.SampleTemperature ?? 0f,
            (Atmospherics.T0C + sm.HeatPenaltyThreshold) * sm.DynamicHeatResistance,
            sample?.WasteMultiplier ?? 0f, sm.EffectiveGasEfficiency * 100f, gases.ToArray());
    }

    private static SupermatterMonitorStatus GetStatus(SupermatterComponent sm)
    {
        if (!sm.Activated)
            return SupermatterMonitorStatus.Inactive;
        if (sm.Delamming)
            return SupermatterMonitorStatus.Delaminating;
        if (sm.Damage >= sm.EmergencyPoint)
            return SupermatterMonitorStatus.Emergency;
        return sm.Damage >= sm.WarningPoint ? SupermatterMonitorStatus.Warning : SupermatterMonitorStatus.Normal;
    }
}
