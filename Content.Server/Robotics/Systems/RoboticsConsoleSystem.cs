using Content.Server.Administration.Logs;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Lock;
using Content.Shared.Database;
using Content.Shared.Robotics;
using Content.Shared.Robotics.Components;
using Content.Shared.Robotics.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Access.Systems;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Station.Systems;

namespace Content.Server.Research.Systems;

/// <summary>
/// Handles UI and state receiving for the robotics control console.
/// <c>BorgTransponderComponent<c/> broadcasts state from the station's borgs to consoles.
/// </summary>
public sealed partial class RoboticsConsoleSystem : SharedRoboticsConsoleSystem
{
    private const string StationBorgFaction = "NanoTrasen";
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private LockSystem _lock = default!;
    [Dependency] private RadioSystem _radio = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private NpcFactionSystem _factions = default!;
    [Dependency] private StationSystem _stations = default!;

    // almost never timing out more than 1 per tick so initialize with that capacity
    private List<string> _removing = new(1);

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<RoboticsConsoleComponent>(RoboticsConsoleUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnOpened);
            subs.Event<RoboticsConsoleDisableMessage>(OnDisable);
            subs.Event<RoboticsConsoleDestroyMessage>(OnDestroy);
            // TODO: camera stuff
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<RoboticsConsoleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // remove cyborgs that havent pinged in a while
            _removing.Clear();
            foreach (var (address, data) in comp.Cyborgs)
            {
                if (now >= data.Timeout)
                    _removing.Add(address);
            }

            // needed to prevent modifying while iterating it
            foreach (var address in _removing)
            {
                comp.Cyborgs.Remove(address);
            }

            if (_removing.Count > 0)
                UpdateUserInterface((uid, comp));
        }
    }

    [SubscribeLocalEvent]
    private void OnPacketReceived(Entity<RoboticsConsoleComponent> ent, ref DeviceNetworkPacketEvent<RoboticsCyborgDataPayload> args)
    {
        var data = args.Data.Data;
        data.Timeout = _timing.CurTime + ent.Comp.Timeout;
        ent.Comp.Cyborgs[args.SenderAddress] = data;

        UpdateUserInterface(ent);
    }

    private void OnOpened(Entity<RoboticsConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUserInterface(ent);
    }


    private void OnDisable(Entity<RoboticsConsoleComponent> ent, ref RoboticsConsoleDisableMessage args)
    {
        if (!TryGetControllableBorg(ent, args.Actor, args.Address, out var data))
            return;

        var payload = new RoboticsCyborgDisablePayload();
        _deviceNetwork.SendPacket(ent.Owner, args.Address, ref payload);

        _adminLogger.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(args.Actor):user} disabled borg {data.Name} with address {args.Address}");
    }

    private void OnDestroy(Entity<RoboticsConsoleComponent> ent, ref RoboticsConsoleDestroyMessage args)
    {
        var now = _timing.CurTime;
        if (now < ent.Comp.NextDestroy || !TryGetControllableBorg(ent, args.Actor, args.Address, out var data))
            return;

        ent.Comp.Cyborgs.Remove(args.Address);

        var payload = new RoboticsCyborgDestroyPayload();
        _deviceNetwork.SendPacket(ent.Owner, args.Address, ref payload);

        var message = Loc.GetString(ent.Comp.DestroyMessage, ("name", data.Name));
        _radio.SendRadioMessage(ent, message, ent.Comp.RadioChannel, ent);
        _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{ToPrettyString(args.Actor):user} destroyed borg {data.Name} with address {args.Address}");

        ent.Comp.NextDestroy = now + ent.Comp.DestroyCooldown;
        Dirty(ent, ent.Comp);
    }

    private bool TryGetControllableBorg(Entity<RoboticsConsoleComponent> ent, EntityUid actor, string address,
        out CyborgControlData data)
    {
        data = default;
        // The transponder list is a cached broadcast. Check the live target and station at the point of action.
        return ent.Comp.AllowBorgControl &&
               !_lock.IsLocked(ent.Owner) &&
               _access.IsAllowed(actor, ent) &&
               ent.Comp.Cyborgs.TryGetValue(address, out data) &&
               data.Timeout > _timing.CurTime &&
               TryComp<DeviceNetworkComponent>(ent, out var device) &&
               _deviceNetwork.FindDevice(device.DeviceNetId, address) is { } borg &&
               !TerminatingOrDeleted(borg) &&
               HasComp<BorgTransponderComponent>(borg) &&
               HasComp<BorgChassisComponent>(borg) &&
               _factions.IsMember(borg, StationBorgFaction) &&
               _stations.GetOwningStation(ent.Owner) is { } station &&
               _stations.GetOwningStation(borg) == station;
    }

    private void UpdateUserInterface(Entity<RoboticsConsoleComponent> ent)
    {
        var state = new RoboticsConsoleState(ent.Comp.Cyborgs, ent.Comp.AllowBorgControl);
        _ui.SetUiState(ent.Owner, RoboticsConsoleUiKey.Key, state);
    }
}
