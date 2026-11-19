using Content.Arcane.Shared.Achievements;
using Content.Arcane.Common.Thunderdome;
using Content.Arcane.Shared.Thunderdome;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Server.Preferences.Managers;
using Content.Shared.Dataset;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Preferences;
using Content.Shared.Station.Systems;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Server.Player;
using Robust.Shared.Containers;

namespace Content.Arcane.Server.Thunderdome;

public sealed partial class ThunderdomeSystem
{
    [Dependency] private MindSystem _minds = default!;
    [Dependency] private GhostSystem _ghosts = default!;
    [Dependency] private IServerPreferencesManager _preferences = default!;
    [Dependency] private StationSpawningSystem _spawning = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private MetaDataSystem _metadata = default!;

    private static readonly ProtoId<LocalizedDatasetPrototype> NameSuffixes = "NamesOperationPrefix";

    private void InitializePlayers()
    {
        SubscribeLocalEvent<ThunderdomePlayerComponent, MobStateChangedEvent>(OnAvatarStateChanged);
        SubscribeLocalEvent<ThunderdomePlayerComponent, EntityTerminatingEvent>(OnAvatarTerminating);
        SubscribeLocalEvent<GhostAttemptHandleEvent>(OnGhostAttempt);
        SubscribeLocalEvent<ThunderdomeArenaItemComponent, MapUidChangedEvent>(OnArenaItemMoved);
        SubscribeLocalEvent<ThunderdomePlayerComponent, MapUidChangedEvent>(OnAvatarMoved);
        SubscribeLocalEvent<ThunderdomeOriginalBodyComponent, MobStateChangedEvent>(OnOriginalBodyStateChanged);
        SubscribeNetworkEvent<ThunderdomeRevivalAcceptEvent>(OnRevivalAccepted);
        _players.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    internal bool CanJoin(ICommonSession session, out Entity<MindComponent> original)
    {
        if (session.Status == SessionStatus.InGame && session.AttachedEntity is { } ghost &&
            HasComp<GhostComponent>(ghost) && _minds.TryGetMind(session.UserId, out var mind) &&
            mind.Value.Comp.CurrentEntity == ghost && mind.Value.Comp.UserId == session.UserId)
        {
            original = mind.Value;
            return true;
        }

        original = default;
        return false;
    }

    internal bool TryJoin(ICommonSession session, EntityUid ruleUid, int choice)
    {
        if (!_configuration.GetCVar(ThunderdomeCVars.Enabled) || !CanJoin(session, out var original) ||
            !TryComp<ThunderdomeRuleComponent>(ruleUid, out var rule) ||
            !GameTicker.IsGameRuleActive(ruleUid) || !_maps.MapExists(rule.Map) ||
            choice < 0 || choice >= rule.WeaponLoadouts.Count || PickSpawn((ruleUid, rule)) is not { } spawn)
            return false;

        HumanoidCharacterProfile? profile = null;
        if (_preferences.TryGetCachedPreferences(session.UserId, out var preferences) &&
            preferences.SelectedCharacter is HumanoidCharacterProfile selected)
        {
            var suffix = Loc.GetString(RobustRandom.Pick(ProtoMan.Index(NameSuffixes).Values));
            profile = selected.WithName($"{selected.Name} {suffix}");
        }

        var returnPosition = _transform.GetMoverCoordinates(original.Comp.CurrentEntity!.Value);
        var avatar = _spawning.SpawnPlayerMob(spawn, null, profile, null);
        _spawning.EquipStartingGear(avatar, rule.Gear);
        _spawning.EquipStartingGear(avatar, rule.WeaponLoadouts[choice].Gear);
        _metadata.AddFlag(avatar, MetaDataFlags.ExtraTransformEvents);
        MarkEquipment(avatar, rule.Map!.Value);
        EnsureComp<NoAchievementsComponent>(avatar);
        EnsureComp<ThunderdomeParticipantComponent>(avatar);

        var temporary = _minds.CreateMind(null, Name(avatar));
        var participant = AddComp<ThunderdomePlayerComponent>(avatar);
        participant.OriginalMind = original.Owner;
        participant.TemporaryMind = temporary.Owner;
        participant.User = session.UserId;
        participant.Rule = ruleUid;
        participant.ReturnPosition = returnPosition;
        participant.OriginalBody = original.Comp.OwnedEntity;
        _minds.TransferTo(temporary.Owner, avatar, mind: temporary.Comp);
        if (temporary.Comp.OwnedEntity != avatar || !CanJoin(session, out var current) || current != original)
        {
            participant.Closing = true;
            _minds.WipeMind(temporary.Owner, temporary.Comp);
            QueueDel(temporary.Owner);
            QueueDel(avatar);
            return false;
        }

        _minds.SetUserId(original.Owner, null, original.Comp);
        _minds.SetUserId(temporary.Owner, session.UserId, temporary.Comp);
        if (original.Comp.VisitingEntity != null)
            _minds.UnVisit(original.Owner, original.Comp);

        if (participant.OriginalBody is { } body && !HasComp<GhostComponent>(body) && !TerminatingOrDeleted(body))
        {
            EnsureComp<ThunderdomeOriginalBodyComponent>(body).Avatar = avatar;
            if (CanReturnToRevivedBody(body))
                RaiseNetworkEvent(new ThunderdomeRevivalOfferEvent(), session);
        }
        return true;
    }

    private void OnOriginalBodyStateChanged(Entity<ThunderdomeOriginalBodyComponent> body, ref MobStateChangedEvent args)
    {
        if (args.OldMobState == MobState.Dead && args.NewMobState != MobState.Dead &&
            TryComp<ThunderdomePlayerComponent>(body.Comp.Avatar, out var participant) &&
            !participant.Closing && _players.TryGetSessionById(participant.User, out var session) &&
            session.AttachedEntity == body.Comp.Avatar)
            RaiseNetworkEvent(new ThunderdomeRevivalOfferEvent(), session);
    }

    private void OnRevivalAccepted(ThunderdomeRevivalAcceptEvent args, EntitySessionEventArgs session)
    {
        if (session.SenderSession.AttachedEntity is not { } avatar || !TryComp<ThunderdomePlayerComponent>(avatar, out var participant) ||
            participant.User != session.SenderSession.UserId || participant.OriginalBody is not { } body ||
            !TryComp<MindComponent>(participant.OriginalMind, out var original) || original.OwnedEntity != body || !CanReturnToRevivedBody(body))
            return;

        Leave((avatar, participant), returnToBody: true);
    }

    private bool CanReturnToRevivedBody(EntityUid body) => !TerminatingOrDeleted(body) &&
        TryComp<MobStateComponent>(body, out var state) && !_mobState.IsDead(body, state);

    private void MarkEquipment(EntityUid parent, MapId map)
    {
        if (!TryComp<ContainerManagerComponent>(parent, out var manager))
            return;

        foreach (var container in _containers.GetAllContainers(parent, manager))
        {
            foreach (var item in container.ContainedEntities)
            {
                EnsureComp<ThunderdomeArenaItemComponent>(item).Map = map;
                _metadata.AddFlag(item, MetaDataFlags.ExtraTransformEvents);
                MarkEquipment(item, map);
            }
        }
    }

    private void OnArenaItemMoved(Entity<ThunderdomeArenaItemComponent> ent, ref MapUidChangedEvent args)
    {
        if (args.OldMapId == ent.Comp.Map && args.NewMapId != ent.Comp.Map)
            QueueDel(ent);
    }

    private void OnAvatarMoved(Entity<ThunderdomePlayerComponent> ent, ref MapUidChangedEvent args)
    {
        if (TryComp<ThunderdomeRuleComponent>(ent.Comp.Rule, out var rule) && args.OldMapId == rule.Map && args.NewMapId != rule.Map)
            QueueDel(ent);
    }

    private void OnAvatarStateChanged(Entity<ThunderdomePlayerComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            OnArenaDeath(ent, args.Origin);
            Leave(ent);
        }
    }

    private void OnAvatarTerminating(Entity<ThunderdomePlayerComponent> ent, ref EntityTerminatingEvent args)
    {
        Leave(ent);
    }

    private void OnGhostAttempt(GhostAttemptHandleEvent args)
    {
        if (args.Handled || args.Mind.OwnedEntity is not { } avatar ||
            !TryComp<ThunderdomePlayerComponent>(avatar, out var participant))
            return;

        Leave((avatar, participant));
        args.Result = true;
        args.Handled = true;
    }

    public override void Shutdown()
    {
        _players.PlayerStatusChanged -= OnPlayerStatusChanged;
        base.Shutdown();
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus != SessionStatus.Disconnected || !_minds.TryGetMind(args.Session.UserId, out var mind) ||
            mind.Value.Comp.OwnedEntity is not { } avatar || !TryComp<ThunderdomePlayerComponent>(avatar, out var participant))
            return;

        Leave((avatar, participant));
    }

    private void Leave(Entity<ThunderdomePlayerComponent> participant, bool returnToBody = false)
    {
        var data = participant.Comp;
        if (data.Closing)
            return;

        data.Closing = true;
        if (TryComp<ThunderdomeOriginalBodyComponent>(data.OriginalBody, out var marker) && marker.Avatar == participant.Owner)
            RemComp(data.OriginalBody!.Value, marker);

        if (_minds.TryGetMind(data.User, out var current) && current.Value.Owner == data.TemporaryMind &&
            TryComp<MindComponent>(data.OriginalMind, out var original) && original.UserId == null &&
            !TerminatingOrDeleted(data.OriginalMind))
        {
            _minds.SetUserId(data.TemporaryMind, null, current.Value.Comp);
            var canReturn = original.OwnedEntity is { } body && !TerminatingOrDeleted(body) && !HasComp<GhostComponent>(body);
            returnToBody &= canReturn && CanReturnToRevivedBody(original.OwnedEntity!.Value);
            if (!returnToBody)
            {
                var interim = original.OwnedEntity;
                _ghosts.SpawnGhost((data.OriginalMind, original), data.ReturnPosition, canReturn, data.User);
                if (interim is { } ghost && HasComp<GhostComponent>(ghost) && original.OwnedEntity != ghost)
                    QueueDel(ghost);
            }
            _minds.SetUserId(data.OriginalMind, data.User, original);
        }

        _minds.WipeMind(data.TemporaryMind);
        QueueDel(data.TemporaryMind);
        if (!TerminatingOrDeleted(participant))
            QueueDel(participant);
    }

    private void CloseArena(EntityUid rule)
    {
        var participants = AllEntityQuery<ThunderdomePlayerComponent>();
        while (participants.MoveNext(out var uid, out var comp))
        {
            if (comp.Rule == rule)
                Leave((uid, comp));
        }
    }
}
