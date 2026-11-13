using Content.Server.Chat.Systems;
using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Station.Components;
using Robust.Shared.Player;

namespace Content.Server.Silicons.StationAi;

public sealed partial class StationAiSystem
{
    [Dependency] private EuiManager _eui = default!;
    [Dependency] private ChatSystem _chat = default!;

    private void InitializeRetirement()
    {
        SubscribeLocalEvent<StationAiHeldComponent, StationAiRetireEvent>(OnRetire);
    }

    private void OnRetire(Entity<StationAiHeldComponent> entity, ref StationAiRetireEvent args)
    {
        if (args.Handled || args.Performer != entity.Owner || !TryComp<ActorComponent>(entity, out var actor) ||
            !TryGetCore(entity, out var core) || _station.GetOwningStation(core.Owner) is not { } station ||
            !_mind.TryGetMind(entity, out var mind, out _) ||
            !TryValidateRetirement(actor.PlayerSession, entity, core.Owner, station, mind, out _))
            return;

        args.Handled = true;
        _eui.OpenEui(new StationAiRetireEui(this, entity, core.Owner, station, mind), actor.PlayerSession);
    }

    private bool TryValidateRetirement(ICommonSession player, EntityUid ai, EntityUid core, EntityUid station,
        EntityUid mindId, out MindComponent mind)
    {
        mind = default!;
        if (player.AttachedEntity != ai || TerminatingOrDeleted(ai) || TerminatingOrDeleted(core) ||
            TerminatingOrDeleted(station) || !_mobState.IsAlive(ai) ||
            !TryGetCore(ai, out var holder) || holder.Owner != core || !TryGetHeld(holder, out var held) || held != ai ||
            _station.GetOwningStation(core) != station ||
            !_mind.TryGetMind(ai, out var current, out var currentMind) || current != mindId ||
            currentMind.UserId != player.UserId || currentMind.IsVisitingEntity || currentMind.OwnedEntity != ai ||
            currentMind.CurrentEntity != ai ||
            !TryComp<StationJobsComponent>(station, out var jobs) || !jobs.JobList.ContainsKey(_stationAiJob.Id) ||
            !_stationJobs.TryGetPlayerJobs(station, player.UserId, out var assignments, jobs) || !assignments.Contains(_stationAiJob))
            return false;

        mind = currentMind;
        return true;
    }

    internal void RetireStationAi(ICommonSession player, EntityUid ai, EntityUid core, EntityUid station, EntityUid mindId)
    {
        if (!TryValidateRetirement(player, ai, core, station, mindId, out var mind))
            return;

        var transitioned = _ghost.OnGhostAttempt(mindId, canReturnGlobal: false, mind: mind);
        var departed = mind.OwnedEntity;
        if (!transitioned || departed == ai || mind.IsVisitingEntity ||
            !TryComp<GhostComponent>(departed, out var ghost) || ghost.CanReturnToBody ||
            !TryGetCore(ai, out var holder) || holder.Owner != core || _station.GetOwningStation(core) != station ||
            !_stationJobs.TryReleasePlayerJob(station, player.UserId, _stationAiJob))
        {
            // A failed ghost spawn can clear the mind. Restore it only if the original AI is still free and installed.
            if ((departed == null || HasComp<GhostComponent>(departed)) && mind.UserId == player.UserId &&
                !TerminatingOrDeleted(ai) && TryGetCore(ai, out var installed) && installed.Owner == core &&
                !_mind.TryGetMind(ai, out _, out _))
                _mind.TransferTo(mindId, ai, createGhost: false, mind: mind);
            return;
        }

        _chat.DispatchStationAnnouncement(core, Loc.GetString("station-ai-retire-announcement", ("ai", Name(ai))),
            playDefaultSound: false);
        QueueDel(ai);
    }

    private sealed class StationAiRetireEui(StationAiSystem system, EntityUid ai, EntityUid core, EntityUid station, EntityUid mind) : BaseEui
    {
        public override void HandleMessage(EuiMessageBase msg)
        {
            if (msg is not ConfirmStationAiRetireMessage || IsShutDown)
            {
                base.HandleMessage(msg);
                return;
            }

            Close();
            system.RetireStationAi(Player, ai, core, station, mind);
        }
    }
}
