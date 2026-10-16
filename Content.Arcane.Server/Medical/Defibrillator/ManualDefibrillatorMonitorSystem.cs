using Content.Arcane.Shared.Medical.Heart;
using Content.Arcane.Shared.Medical.Defibrillator;
using Content.Shared.Body;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.Medical.Defibrillator;

public sealed partial class ManualDefibrillatorMonitorSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private ActivatableUISystem _activatableUi = default!;
    [Dependency] private ItemToggleSystem _itemToggle = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ManualDefibrillatorSystem _manualDefibrillator = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    public static readonly ProtoId<OrganCategoryPrototype> HeartCategory = "Heart";
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();
        _nextUpdate = _timing.CurTime + UpdateInterval;

        SubscribeLocalEvent<ManualDefibrillatorComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ManualDefibrillatorComponent, BoundUIClosedEvent>(OnUiClosed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        var eqe = EntityQueryEnumerator<ManualDefibrillatorComponent>();
        while (eqe.MoveNext(out var uid, out var defibrillator))
        {
            UpdateMonitor((uid, defibrillator));
        }
    }

    private void OnAfterInteract(Entity<ManualDefibrillatorComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled
            || _itemToggle.IsActivated(ent.Owner)
            || !args.CanReach
            || args.Target is not { } target
            || !HasComp<BodyComponent>(target)
            || !TryComp<ActivatableUIComponent>(ent, out var activatable))
            return;

        if (!_ui.IsUiOpen(ent.Owner, ManualDefibrillatorUiKey.Key, args.User))
            _activatableUi.InteractUI(args.User, ent.Owner, activatable, ManualDefibrillatorUiKey.Key);

        if (!_ui.IsUiOpen(ent.Owner, ManualDefibrillatorUiKey.Key, args.User))
            return;

        ent.Comp.MonitoredBody = target;
        ent.Comp.ShockUntil = TimeSpan.Zero;
        args.Handled = true;
        UpdateMonitor(ent);
    }

    private void OnUiClosed(Entity<ManualDefibrillatorComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!args.UiKey.Equals(ManualDefibrillatorUiKey.Key)
            || _ui.IsUiOpen(ent.Owner, ManualDefibrillatorUiKey.Key))
            return;

        ResetMonitor(ent);
    }

    private void UpdateMonitor(Entity<ManualDefibrillatorComponent> ent)
    {
        if (ent.Comp.MonitoredBody is not { } body)
            return;

        if (Deleted(body)
            || !_interaction.InRangeUnobstructed(ent.Owner, body))
        {
            ResetMonitor(ent);
            return;
        }

        var bpm = _body.GetOrgan(body, HeartCategory) is { } heartUid
            && TryComp<HeartRateComponent>(heartUid, out var heart)
            ? heart.CurrentRate
            : 0f;
        SetMonitorState(ent, bpm);
    }

    public void ShowShock(Entity<ManualDefibrillatorComponent> ent, EntityUid patient, float bpm)
    {
        if (!_ui.IsUiOpen(ent.Owner, ManualDefibrillatorUiKey.Key))
            return;

        ent.Comp.MonitoredBody = patient;
        ent.Comp.ShockUntil = _timing.CurTime + UpdateInterval;
        SetMonitorState(ent, bpm);
    }

    private void ResetMonitor(Entity<ManualDefibrillatorComponent> ent)
    {
        ent.Comp.MonitoredBody = null;
        ent.Comp.ShockUntil = TimeSpan.Zero;
        SetMonitorState(ent, null);
    }

    private void SetMonitorState(Entity<ManualDefibrillatorComponent> ent, float? bpm)
    {
        ProtoId<PulseStatePrototype> pulse = _timing.CurTime < ent.Comp.ShockUntil ? "Pulse3000" : GetPulseState(bpm ?? 0f);
        // The heart keeps its exact rate; the monitor only transmits changes visible in its UI.
        bpm = bpm is { } rate ? MathF.Round(rate) : null;
        if (ent.Comp.PulseState == pulse && ent.Comp.DisplayedBpm == bpm)
            return;

        ent.Comp.PulseState = pulse;
        ent.Comp.DisplayedBpm = bpm;
        _manualDefibrillator.UpdateUi(ent);
    }

    private static ProtoId<PulseStatePrototype> GetPulseState(float bpm)
    {
        return bpm switch
        {
            <= 0f => "Pulse0",
            <= 20f => "Pulse20",
            <= 30f => "Pulse30",
            <= 40f => "Pulse40",
            <= 50f => "Pulse50",
            <= 60f => "Pulse60",
            <= 70f => "Pulse70",
            <= 80f => "Pulse80",
            <= 100f => "Pulse100",
            <= 120f => "Pulse120",
            <= 140f => "Pulse140",
            <= 160f => "Pulse160",
            <= 180f => "Pulse180",
            <= 200f => "Pulse200",
            <= 220f => "Pulse220",
            <= 240f => "Pulse240",
            <= 260f => "Pulse260",
            <= 280f => "Pulse280",
            <= 300f => "Pulse300",
            <= 320f => "Pulse320",
            <= 360f => "Pulse360",
            <= 400f => "Pulse400",
            <= 450f => "Pulse450",
            <= 999f => "Pulse450",
            _ => "Pulse3000",
        };
    }

}
