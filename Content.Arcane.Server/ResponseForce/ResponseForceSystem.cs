using System.Linq;
using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Humanoid.Components;
using Content.Server.Humanoid.Systems;
using Content.Server.RandomMetadata;
using Content.Shared.Spawners.Components;
using Content.Shared.Station.Systems;
using Content.Arcane.Shared.ResponseForce;
using Content.Arcane.Common.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.Ghost.Roles.Raffles;
using Content.Shared.Storage;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Content.Arcane.Server.ResponseForce;

public sealed partial class ResponseForceSystem : EntitySystem
{
    [Dependency] private MapSystem _mapSystem = default!;
    [Dependency] private MapLoaderSystem _map = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private ChatSystem _chatSystem = default!;
    [Dependency] private StationSystem _stationSystem = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private ISerializationManager _serialization = default!;
    [Dependency] private IConfigurationManager _configurationManager = default!;
    [Dependency] private IComponentFactory _componentFactory = default!;
    [Dependency] private RandomHumanoidSystem _randomHumanoid = default!;

    private ISawmill _log = default!;

    // Per round state of this world, reset on round restart cleanup.
    [ViewVariables]
    private readonly List<ResponseForceHistory> _calledEvents = new();

    [ViewVariables]
    private TimeSpan _lastCallTime = TimeSpan.Zero;

    [ViewVariables]
    private MapId? _shipyardMap;

    [ViewVariables]
    private float _shuttleIndex;

    private TimeSpan CallDelay => TimeSpan.FromMinutes(_configurationManager.GetCVar(ArcaneCVars.ResponseForceDelay));

    public override void Initialize()
    {
        base.Initialize();

        _log = Logger.GetSawmill("responseforce.call");

        SubscribeLocalEvent<ResponseForceComponent, MapInitEvent>(OnMapInit, after: [typeof(RandomMetadataSystem)]);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnd);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnCleanup);
//        SubscribeLocalEvent<BlobCriticalStageEvent>(OnBlobCritical);
    }

    private void SetupShipyard()
    {
        if (_shipyardMap != null && _mapSystem.MapExists(_shipyardMap))
            return;

        var mapEntity = _mapSystem.CreateMap(out var mapId);
        _shipyardMap = mapId;

        _mapSystem.SetPaused(mapEntity, false);
        _shuttleIndex = 0f;
    }

    private void CleanupShipyard()
    {
        if (_shipyardMap == null || !_mapSystem.MapExists(_shipyardMap.Value))
        {
            _shipyardMap = null;
            _shuttleIndex = 0f;
            return;
        }

        _mapSystem.DeleteMap(_shipyardMap.Value);
        _shipyardMap = null;
        _shuttleIndex = 0f;
    }

/* TODO: Do this.
    private void OnBlobCritical(BlobCriticalStageEvent ev)
    {
        var responseEvent = new BlobCriticalStageEvent
        {
            Station = ev.Station,
        };

        RaiseLocalEvent(ev.Station, responseEvent);
    }
*/

    private void OnMapInit(EntityUid uid, ResponseForceComponent component, MapInitEvent args)
    {
        foreach (var entry in component.Components.Values)
        {
            var comp = (Component) _serialization.CreateCopy(entry.Component, notNullableOverride: true);
            AddComp(uid, comp);
        }
    }

    public TimeSpan DelayTime
    {
        get
        {
            var ct = _gameTicker.RoundDuration();
            var lastUsedTime = _lastCallTime + CallDelay;
            return ct > lastUsedTime ? TimeSpan.Zero : lastUsedTime - ct;
        }
    }

    /// <summary>
    /// Calls ResponseForce, creating new map with a shuttle, and spawning on it ResponseForces.
    /// </summary>
    /// <param name="protoId"> ResponseForcePrototype ID.</param>
    /// <param name="source"> Source of the call.</param>
    /// <param name="forceCountExtra"> How many extra ResponseForce will be forced to spawn.</param>
    /// <param name="forceCall"> If true, cooldown will be ignored.</param>
    /// <returns>Success, or the reason nothing was called.</returns>
    public ResponseForceCallResult CallResponseForce(ProtoId<ResponseForceTeamPrototype> protoId, string source = "Unknown", int? forceCountExtra = null, bool forceCall = false)
    {
        if (!_prototypes.TryIndex(protoId, out var prototype))
        {
            _log.Error("Wrong ResponseForceTeamPrototype ID!");
            return ResponseForceCallResult.UnknownTeam;
        }

        // Checked before the shuttle loads: GetCount divides by it.
        if (prototype.SpawnPerPlayers <= 0)
        {
            _log.Error($"ResponseForceTeam {prototype.ID} has spawnPerPlayers {prototype.SpawnPerPlayers}, it must be positive.");
            return ResponseForceCallResult.InvalidTeam;
        }

        if (_gameTicker.RunLevel != GameRunLevel.InRound)
        {
            _log.Warning("Can't call ResponseForce while not in the round.");
            return ResponseForceCallResult.NotInRound;
        }

        var currentTime = _gameTicker.RoundDuration();

#if !DEBUG
        if (_lastCallTime + CallDelay > currentTime && !forceCall)
        {
            _log.Info("Tried to call ResponseForce when it's on cooldown.");
            return ResponseForceCallResult.OnCooldown;
        }
#endif

        var shuttle = SpawnShuttle(prototype.ShuttlePath);
        if (shuttle == null)
        {
            _log.Error("Failed to load ResponseForce shuttle!");
            return ResponseForceCallResult.ShuttleLoadFailed;
        }

        // A failed shuttle load does not use up the cooldown.
        _lastCallTime = currentTime;

        SpawnGhostRole(prototype, shuttle.Value, forceCountExtra);
        DispatchAnnouncement(prototype);

        _log.Info($"Successfully called {prototype.ID} ResponseForceTeam. Source: {source}");

        _calledEvents.Add(new ResponseForceHistory { Event = prototype.ResponseForceName, RoundTime = currentTime, WhoCalled = source });

        return ResponseForceCallResult.Success;
    }

    private EntityUid SpawnEntity(string? protoName, EntityCoordinates coordinates, ResponseForceTeamPrototype responseForce)
    {
        if (protoName == null)
            return EntityUid.Invalid;

        // A random humanoid spawner deletes itself and spawns the humanoid on map init; set up that humanoid
        // directly, the ghost role and raffle below have to land on the mob players take over.
        var uid = _prototypes.TryIndex<EntityPrototype>(protoName, out var spawnerProto) &&
                  spawnerProto.TryGetComponent<RandomHumanoidSpawnerComponent>(out var humanoidSpawner, _componentFactory) &&
                  humanoidSpawner.SettingsPrototypeId is { } settings
            ? _randomHumanoid.SpawnRandomHumanoid(settings, coordinates, spawnerProto.Name)
            : Spawn(protoName, coordinates);

        EnsureComp<ResponseForceComponent>(uid);

        // If entity is a GhostRoleMobSpawner, it's child prototype is valid AND
        // has GhostRoleComponent, clone this component and add it to the parent.
        // This is necessary for SpawnMarkers that don't have GhostRoleComp in prototype.
        if (TryComp<GhostRoleMobSpawnerComponent>(uid, out var mobSpawnerComponent) &&
            mobSpawnerComponent.Prototype != null &&
            _prototypes.TryIndex<EntityPrototype>(mobSpawnerComponent.Prototype, out var spawnObj) &&
            spawnObj.TryGetComponent<GhostRoleComponent>(out var tplGhostRoleComponent, _componentFactory))
        {
            var comp = _serialization.CreateCopy(tplGhostRoleComponent, notNullableOverride: true);

            comp.RaffleConfig ??= new();
            comp.RaffleConfig.Settings = new ProtoId<GhostRoleRaffleSettingsPrototype>(responseForce.RaffleSettings);

            AddComp(uid, comp);
        }

        // The team decides the raffle of all its roles, including ones whose settings bring their own.
        if (TryComp<GhostRoleComponent>(uid, out var ghostRole))
        {
            ghostRole.RaffleConfig ??= new();
            ghostRole.RaffleConfig.Settings = new ProtoId<GhostRoleRaffleSettingsPrototype>(responseForce.RaffleSettings);
        }

        return uid;
    }

    public int GetCount(ResponseForceTeamPrototype proto, int? plrCount = null)
    {
        return ((plrCount ?? _playerManager.PlayerCount) + proto.SpawnPerPlayers) / proto.SpawnPerPlayers;
    }

    private void SpawnGhostRole(ResponseForceTeamPrototype proto, EntityUid shuttle, int? forceCountExtra = null)
    {
        // Find all spawn points on the shuttle, add them in list
        var spawns = new List<EntityCoordinates>();
        var query = EntityQueryEnumerator<SpawnPointComponent, MetaDataComponent, TransformComponent>();
        while (query.MoveNext(out _, out var meta, out var xform))
        {
            if (meta.EntityPrototype!.ID != proto.SpawnMarker)
                continue;

            if (xform.GridUid != shuttle)
                continue;

            spawns.Add(xform.Coordinates);
        }

        if (spawns.Count == 0)
        {
            _log.Warning("Shuttle has no valid spawns for ResponseForce! Making something up...");
            spawns.Add(Transform(shuttle).Coordinates);
        }

        SpawnGuaranteed(proto, spawns);
        SpawnResponseForce(proto, spawns, forceCountExtra);
    }

    private void SpawnGuaranteed(ResponseForceTeamPrototype proto, List<EntityCoordinates> spawns)
    {
        // If responseForceSpawn is empty, we can't continue
        if (proto.GuaranteedSpawn.Count == 0)
            return;

        // Spawn Guaranteed ResponseForce from the prototype.
        var toSpawnGuaranteed = EntitySpawnCollection.GetSpawns(proto.GuaranteedSpawn, _random);

        foreach (var mob in toSpawnGuaranteed)
        {
            var spawned = SpawnEntity(mob, _random.Pick(spawns), proto);
            _log.Info($"Successfully spawned {ToPrettyString(spawned)} Static ResponseForce.");
        }
    }

    private void SpawnResponseForce(ResponseForceTeamPrototype proto, List<EntityCoordinates> spawns, int? forceCountExtra)
    {
        // If responseForceSpawn is empty, we can't continue
        if (proto.ResponseForceSpawn.Count == 0)
            return;

        // Count how many other forces there should be, capped by MaxRolesAmount. The guaranteed roles come on top.
        var countExtra = GetCount(proto);
        countExtra = Math.Min(countExtra, proto.MaxRolesAmount);

        // A forced count is capped the same way.
        if (forceCountExtra is >= 0)
            countExtra = Math.Min(forceCountExtra.Value, proto.MaxRolesAmount);

        // Either zero or bigger than zero, no negatives
        countExtra = Math.Max(0, countExtra);

        // Spawn Guaranteed Response Force from the prototype.
        // If all mobs from the list are spawned and we still have free slots, restart the cycle again.
        while (countExtra > 0)
        {
            var toSpawnForces = EntitySpawnCollection.GetSpawns(proto.ResponseForceSpawn, _random);
            if (toSpawnForces.Count == 0)
                break;

            foreach (var mob in toSpawnForces.Where(_ => countExtra > 0))
            {
                countExtra--;
                var spawned = SpawnEntity(mob, _random.Pick(spawns), proto);
                _log.Info($"Successfully spawned {ToPrettyString(spawned)} Opt-in ResponseForce.");
            }
        }
    }

    /// <summary>
    /// Spawns shuttle for Response Force on a new map.
    /// </summary>
    /// <param name="shuttlePath"></param>
    /// <returns>Grid's entity of the shuttle.</returns>
    private EntityUid? SpawnShuttle(string shuttlePath)
    {
        SetupShipyard();

        if (!_map.TryLoadGrid(_shipyardMap!.Value, new ResPath(shuttlePath), out var grid, offset: new Vector2(500f + _shuttleIndex, 1f)))
            return null;

        _shuttleIndex += grid.Value.Comp.LocalAABB.Width + 1;

        return grid;
    }

    private void DispatchAnnouncement(ResponseForceTeamPrototype proto)
    {
        var stations = _stationSystem.GetStations();

        if (stations.Count == 0)
            return;

        // If we don't have title or text for the announcement, we can't make the announcement.
        if (proto.AnnouncementText == null || proto.AnnouncementTitle == null)
            return;

        foreach (var station in stations)
        {
            _chatSystem.DispatchStationAnnouncement(station,
                Loc.GetString(proto.AnnouncementText),
                Loc.GetString(proto.AnnouncementTitle),
                playDefaultSound: true,
                announcementSound: proto.Sound);
        }
    }

    private void OnRoundEnd(ref RoundEndTextAppendEvent ev)
    {
        foreach (var calledEvent in _calledEvents)
        {
            ev.AddLine(Loc.GetString("response-force-system-round-end",
                ("team", Loc.GetString(calledEvent.Event)),
                ("time", calledEvent.RoundTime.ToString(@"hh\:mm")),
                ("who", calledEvent.WhoCalled)));
        }
    }

    private void OnCleanup(RoundRestartCleanupEvent ev)
    {
        _calledEvents.Clear();
        _lastCallTime = TimeSpan.Zero;

        CleanupShipyard();
    }
}
