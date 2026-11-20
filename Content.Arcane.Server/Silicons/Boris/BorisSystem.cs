using System.Linq;
using Content.Arcane.Shared.Silicons.Boris;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Server.Silicons.Laws;
using Content.Shared.Actions;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.PowerCell;
using Content.Shared.Radio.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Station.Systems;
using Content.Shared.Verbs;
using Content.Shared.Xenoborgs.Components;
using Content.Trauma.Common.Silicons.Borgs;
using Content.Trauma.Shared.Silicons.Borgs.Components;
using Content.Trauma.Shared.Silicons.Laws;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.Silicons.Boris;

public sealed partial class BorisSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private GhostSystem _ghost = default!;
    [Dependency] private SiliconLawSystem _laws = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private PowerCellSystem _power = default!;
    [Dependency] private SharedStationAiSystem _stationAi = default!;
    [Dependency] private StationSystem _stations = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly EntProtoId ReturnAction = "ActionBackToBorisCore";
    private readonly List<Entity<BorisControllerComponent>> _pendingReturns = new();
    private readonly Dictionary<EntityUid, List<BorisDevice>> _openDeviceLists = new();
    private TimeSpan _nextValidation;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BorisControllerComponent, GetSiliconLawsEvent>(OnGetLaws, before: [typeof(SiliconLawSystem)]);
        Subs.BuiEvents<StationAiHeldComponent>(BorisUiKey.Devices, events =>
        {
            events.Event<BoundUIOpenedEvent>(OnDevicesOpened);
            events.Event<BoundUIClosedEvent>(OnDevicesClosed);
            events.Event<BorisDeviceMessage>(OnDeviceMessage);
        });
    }

    [SubscribeLocalEvent]
    private void OnInstall(Entity<AiRemoteBrainComponent> ent, ref BorgChassisInteractAfterEvent args)
    {
        if (args.Handled || !TryComp<BorgChassisComponent>(args.Chassis, out var chassis) ||
            chassis.BrainEntity != null || !IsStationBorg(args.Chassis) || IsOccupied(args.Chassis))
            return;

        args.Handled = _containers.Insert(ent.Owner, chassis.BrainContainer);
    }

    [SubscribeLocalEvent]
    private void OnBoardInserted(Entity<AiRemoteBrainComponent> ent, ref BorgBrainInsertedEvent args)
    {
        if (!IsStationBorg(args.Chassis))
            return;

        var controller = EnsureComp<BorisControllerComponent>(args.Chassis);
        controller.Board = ent.Owner;
    }

    [SubscribeLocalEvent]
    private void OnBoardRemoved(Entity<AiRemoteBrainComponent> ent, ref BorgBrainRemovedEvent args)
    {
        if (!TryComp<BorisControllerComponent>(args.Chassis, out var controller) || controller.Board != ent.Owner)
            return;

        ReturnToAi((args.Chassis, controller));
        RemComp(args.Chassis, controller);
    }

    [SubscribeLocalEvent]
    private void OnOpen(Entity<StationAiHeldComponent> ent, ref OpenBorisDevicesEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner || !TryGetAi(ent, out _) ||
            !TryComp<ActorComponent>(ent, out var actor))
            return;

        args.Handled = true;
        _ui.TryToggleUi(ent.Owner, BorisUiKey.Devices, actor.PlayerSession);
    }

    private void OnDevicesOpened(Entity<StationAiHeldComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.Actor == ent.Owner && TryGetAi(ent, out var core))
            RefreshDevices(ent, core);
    }

    private void OnDevicesClosed(Entity<StationAiHeldComponent> ent, ref BoundUIClosedEvent args) =>
        _openDeviceLists.Remove(ent.Owner);

    private void RefreshDevices(EntityUid ai, Entity<StationAiCoreComponent?> core)
    {
        var devices = new List<BorisDevice>();
        if (core.Comp is { Remote: true })
        {
            var query = EntityQueryEnumerator<BorisControllerComponent>();
            while (query.MoveNext(out var borg, out var controller))
            {
                if (CanControl(core, (borg, controller)))
                    devices.Add(new BorisDevice(GetNetEntity(borg), Name(borg)));
            }
        }

        devices = devices.OrderBy(device => device.Name).ThenBy(device => device.Target).ToList();
        if (_openDeviceLists.TryGetValue(ai, out var previous) && previous.SequenceEqual(devices))
            return;

        _openDeviceLists[ai] = devices;
        _ui.SetUiState(ai, BorisUiKey.Devices, new BorisDeviceState(devices));
    }

    private void OnDeviceMessage(Entity<StationAiHeldComponent> ent, ref BorisDeviceMessage args)
    {
        if (args.Actor != ent.Owner || !TryGetAi(ent, out var core))
            return;

        var target = GetEntity(args.Target);
        if (core.Comp is not { Remote: true } ||
            !TryComp<BorisControllerComponent>(target, out var controller) || !CanControl(core, (target, controller)))
        {
            RefreshDevices(ent, core);
            return;
        }

        switch (args.Action)
        {
            case BorisDeviceAction.View when core.Comp?.RemoteEntity is { } eye:
                _transform.SetCoordinates(eye, Transform(target).Coordinates);
                break;
            case BorisDeviceAction.Control:
                TryControl(ent, target);
                break;
        }
    }

    [SubscribeLocalEvent]
    private void OnControlVerb(Entity<BorisControllerComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!TryGetAi(args.User, out var core) || core.Comp is not { Remote: true } || !CanControl(core, ent))
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("boris-control"),
            Act = () => TryControl(user, ent),
        });
    }

    public bool TryControl(EntityUid ai, EntityUid borg)
    {
        if (!TryGetAi(ai, out var core) || core.Comp is not { Remote: true } ||
            !TryComp<BorisControllerComponent>(borg, out var controller) ||
            !CanControl(core, (borg, controller)) || !_mind.TryGetMind(ai, out var mindId, out var mind) ||
            mind.OwnedEntity != ai || mind.VisitingEntity != null)
            return false;

        controller.Ai = ai;
        controller.Core = core.Owner;
        controller.Mind = mindId;
        if (TryComp<IntrinsicRadioTransmitterComponent>(ai, out var aiTransmitter) &&
            TryComp<IntrinsicRadioTransmitterComponent>(borg, out var transmitter))
        {
            controller.TransmitChannels = [.. transmitter.Channels];
            transmitter.Channels.UnionWith(aiTransmitter.Channels);
        }
        if (TryComp<ActiveRadioComponent>(ai, out var aiRadio) && TryComp<ActiveRadioComponent>(borg, out var radio))
        {
            controller.ReceiveChannels = [.. radio.Channels];
            radio.Channels.UnionWith(aiRadio.Channels);
        }

        _ui.CloseUi(ai, BorisUiKey.Devices, ai);
        _stationAi.SwitchRemoteEntityMode(core, false);
        _mind.TransferTo(mindId, borg, ghostCheckOverride: true, mind: mind);
        _actions.AddAction(borg, ref controller.ReturnAction, ReturnAction);
        return true;
    }

    private bool TryGetAi(EntityUid ai, out Entity<StationAiCoreComponent?> core)
    {
        core = default;
        return !TerminatingOrDeleted(ai) && HasComp<StationAiHeldComponent>(ai) &&
               !_mob.IsIncapacitated(ai) && _stationAi.TryGetCore(ai, out core) &&
               !TerminatingOrDeleted(core.Owner);
    }

    private bool CanControl(Entity<StationAiCoreComponent?> core, Entity<BorisControllerComponent> borg)
    {
        return IsUsableBorg(borg) && borg.Comp.Mind == null && !IsOccupied(borg) &&
               IsOnSameStation(core.Owner, borg.Owner);
    }

    private bool IsOnSameStation(EntityUid core, EntityUid borg) =>
        _stations.GetOwningStation(core) is { } station && _stations.GetOwningStation(borg) == station;

    private bool IsUsableBorg(Entity<BorisControllerComponent> borg)
    {
        return !TerminatingOrDeleted(borg) && !TerminatingOrDeleted(borg.Comp.Board) &&
               TryComp<BorgChassisComponent>(borg, out var chassis) && chassis.BrainEntity == borg.Comp.Board &&
               (!TryComp<BorgSwitchableTypeComponent>(borg, out var selection) || selection.SelectedBorgType != null) &&
               IsStationBorg(borg) && !_mob.IsIncapacitated(borg) && _power.HasDrawCharge(borg.Owner);
    }

    private bool IsStationBorg(EntityUid borg) =>
        HasComp<SlavedBorgComponent>(borg) && !HasComp<XenoborgComponent>(borg);

    private bool IsOccupied(EntityUid entity) =>
        _mind.TryGetMind(entity, out _, out _) || HasComp<ActorComponent>(entity) || HasComp<VisitingMindComponent>(entity);

    private void OnGetLaws(Entity<BorisControllerComponent> ent, ref GetSiliconLawsEvent args)
    {
        if (args.Handled || ent.Comp.Ai is not { } ai || TerminatingOrDeleted(ai))
            return;

        args.Laws = _laws.GetLaws(ai);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnReturnAction(Entity<BorisControllerComponent> ent, ref ReturnFromBorisEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner || ent.Comp.Mind == null)
            return;

        args.Handled = true;
        ReturnToAi(ent);
    }

    [SubscribeLocalEvent]
    private void OnBorgTerminating(Entity<BorisControllerComponent> ent, ref EntityTerminatingEvent args) =>
        ReturnToAi(ent);

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<BorisControllerComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive)
            ReturnToAi(ent);
    }

    public override void Update(float frameTime)
    {
        if (_timing.CurTime < _nextValidation)
            return;

        _nextValidation = _timing.CurTime + TimeSpan.FromSeconds(0.5);
        var query = EntityQueryEnumerator<BorisControllerComponent>();
        while (query.MoveNext(out var uid, out var controller))
        {
            if (controller.Mind is not { } mind)
                continue;

            if (!HasComp<ActorComponent>(uid) || !IsUsableBorg((uid, controller)) ||
                controller.Ai is not { } ai || !TryGetAi(ai, out var core) || core.Owner != controller.Core ||
                !IsOnSameStation(core.Owner, uid) ||
                !TryComp<MindComponent>(mind, out var mindComp) || mindComp.OwnedEntity != uid)
                _pendingReturns.Add((uid, controller));
        }

        foreach (var ent in _pendingReturns)
            ReturnToAi(ent);
        _pendingReturns.Clear();

        foreach (var ai in _openDeviceLists.Keys.ToArray())
        {
            if (_ui.IsUiOpen(ai, BorisUiKey.Devices, ai) && TryGetAi(ai, out var core))
                RefreshDevices(ai, core);
            else
                _openDeviceLists.Remove(ai);
        }
    }

    private void ReturnToAi(Entity<BorisControllerComponent> ent)
    {
        if (ent.Comp.Ai is not { } ai || ent.Comp.Mind is not { } mindId)
            return;

        var core = ent.Comp.Core;
        ent.Comp.Ai = ent.Comp.Core = ent.Comp.Mind = null;
        if (ent.Comp.TransmitChannels is { } transmit && TryComp<IntrinsicRadioTransmitterComponent>(ent, out var transmitter))
            transmitter.Channels = transmit;
        if (ent.Comp.ReceiveChannels is { } receive && TryComp<ActiveRadioComponent>(ent, out var radio))
            radio.Channels = receive;
        ent.Comp.TransmitChannels = ent.Comp.ReceiveChannels = null;
        if (!TerminatingOrDeleted(ent))
            _actions.RemoveAction(ent.Owner, ent.Comp.ReturnAction);
        ent.Comp.ReturnAction = null;

        if (TryComp<MindComponent>(mindId, out var mind) && mind.OwnedEntity == ent.Owner)
        {
            var originalBrain = !TerminatingOrDeleted(ai) && !IsOccupied(ai) ? ai : (EntityUid?) null;
            _mind.TransferTo(mindId, originalBrain, ghostCheckOverride: true, createGhost: !EntityManager.ShuttingDown, mind: mind);
            if (originalBrain != null && _mob.IsDead(ai) && !EntityManager.ShuttingDown)
                _ghost.OnGhostAttempt(mindId, canReturnGlobal: true, mind: mind);
        }

        if (core is { } coreUid && !TerminatingOrDeleted(coreUid) &&
            TryGetAi(ai, out var liveCore) && liveCore.Owner == coreUid)
            _stationAi.SwitchRemoteEntityMode(liveCore, true);
    }
}
