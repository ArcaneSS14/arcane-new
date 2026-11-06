using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Access.Systems;
using Content.Shared.Popups;
using Content.Shared.Research.Components;
using Content.Shared.Research.Systems;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.Random; // Arcane

using Content.Shared.Research.Prototypes; // Arcane

// Arcane-Start
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Examine;
// Arcane-End

namespace Content.Server.Research.Systems
{
    [UsedImplicitly]
    public sealed partial class ResearchSystem : SharedResearchSystem
    {
        [Dependency] private ChatSystem _chat = default!; // Arcane

        [Dependency] private IAdminLogManager _adminLog = default!;
        [Dependency] private IGameTiming _timing = default!;
        [Dependency] private AccessReaderSystem _accessReader = default!;
        [Dependency] private EntityLookupSystem _lookup = default!;
        [Dependency] private UserInterfaceSystem _uiSystem = default!;
        [Dependency] private SharedPopupSystem _popup = default!;
        [Dependency] private RadioSystem _radio = default!;
        [Dependency] private IRobustRandom _random = default!; // Arcane

        public override void Initialize()
        {
            base.Initialize();
            InitializeClient();
            InitializeConsole();
            InitializeSource();
            InitializeServer();
            // Arcane-Start
            InitializeArcane();
            InitializeExperiments();
            InitializeDiscovery();
            // Arcane-End

            SubscribeLocalEvent<TechnologyDatabaseComponent, ResearchRegistrationChangedEvent>(OnDatabaseRegistrationChanged);
        }

        /// <summary>
        /// Gets a server based on its unique numeric id.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="id"></param>
        /// <param name="serverUid"></param>
        /// <param name="serverComponent"></param>
        /// <returns></returns>
        public bool TryGetServerById(EntityUid client, int id, [NotNullWhen(true)] out EntityUid? serverUid, [NotNullWhen(true)] out ResearchServerComponent? serverComponent)
        {
            serverUid = null;
            serverComponent = null;

            var query = GetServers(client);
            foreach (var (uid, server) in query)
            {
                if (server.Id != id)
                    continue;
                serverUid = uid;
                serverComponent = server;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the names of all the servers.
        /// </summary>
        /// <returns></returns>
        public string[] GetServerNames(EntityUid client)
        {
            return GetServers(client).Select(x => x.Comp.ServerName).ToArray();
        }

        /// <summary>
        /// Gets the ids of all the servers
        /// </summary>
        /// <returns></returns>
        public int[] GetServerIds(EntityUid client)
        {
            return GetServers(client).Select(x => x.Comp.Id).ToArray();
        }

        public List<Entity<ResearchServerComponent>> GetServers(EntityUid client) // Arcane-Edit
        {
            var clientXform = Transform(client);
            if (clientXform.GridUid is not { } grid)
                return new List<Entity<ResearchServerComponent>>(); // Arcane-Edit

            // Arcane-Edit-Start
            var servers = new HashSet<Entity<ResearchServerComponent>>();
            _lookup.GetGridEntities(grid, servers);

            return servers
                .OrderBy(ent => ent.Comp.Id)
                .ToList();
            // Arcane-Edit-End
        }

        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<ResearchServerComponent>();
            while (query.MoveNext(out var uid, out var server))
            {
                if (server.NextUpdateTime > _timing.CurTime)
                    continue;
                server.NextUpdateTime = _timing.CurTime + server.ResearchConsoleUpdateTime;

                UpdateServer(uid, (int) server.ResearchConsoleUpdateTime.TotalSeconds, server);
            }
        }

        // Arcane-Start
        private string GetLocalizedPointType(string type)
        {
            return LocalizeResearchPointType(type);
        }

        public int UnlockAllTechnologiesOnServer(EntityUid uid, ResearchServerComponent? server = null, TechnologyDatabaseComponent? database = null)
        {
            if (!Resolve(uid, ref server, ref database, false))
                return 0;

            var authority = GetNetworkAuthority(uid, server);
            if (authority != uid)
            {
                uid = authority;
                server = null;
                database = null;

                if (!Resolve(uid, ref server, ref database, false))
                    return 0;
            }

            var unlocked = 0;
            foreach (var technology in ProtoMan.EnumeratePrototypes<TechnologyPrototype>())
            {
                if (!database.SupportedDisciplines.Contains(technology.Discipline))
                    database.SupportedDisciplines.Add(technology.Discipline);

                if (!database.RevealedTechnologies.Contains(technology.ID))
                    database.RevealedTechnologies.Add(technology.ID);

                if (database.ResearchedTechnologies.Contains(technology.ID))
                    continue;

                database.ResearchedTechnologies.Add(technology.ID);
                unlocked++;
            }

            if (unlocked == 0)
                return 0;

            var previouslyUnlockedRecipes = database.UnlockedRecipes.ToHashSet();
            RecalculateTechnologyState(uid, database);
            UpdateTechnologyCards(uid, database);
            Dirty(uid, database);

            var newlyUnlockedRecipes = database.UnlockedRecipes.Except(previouslyUnlockedRecipes).Select(recipe => recipe.Id).ToList();
            var ev = new TechnologyDatabaseModifiedEvent(newlyUnlockedRecipes);
            RaiseLocalEvent(uid, ref ev);

            LogNetworkEvent(uid,
                "admin",
                Loc.GetString("research-netlog-admin-unlocked-all-technologies", ("user", Loc.GetString("research-netlog-user-admin"))),
                null,
                server);

            return unlocked;
        }

        public int AddServerPointsByType(EntityUid uid, string type, int amount, ResearchServerComponent? server = null)
        {
            ModifyServerPoints(uid, type, amount, server);
            var balance = GetPointBalance(uid, type, server);
            var localizedType = GetLocalizedPointType(type);
            LogNetworkEvent(uid,
                "admin",
                Loc.GetString("research-netlog-admin-added-points",
                    ("user", Loc.GetString("research-netlog-user-admin")),
                    ("type", localizedType),
                    ("amount", amount),
                    ("balance", balance)),
                null,
                server);
            return balance;
        }
        // Arcane-End

        // Arcane-Start
        private void InitializeArcane()
        {
            SubscribeLocalEvent<ResearchServerComponent, ExaminedEvent>(OnServerExamined);
        }

        private void SayTechnologyUnlocked(EntityUid console, TechnologyPrototype technology, string cost, string approver)
        {
            var message = Loc.GetString("research-console-unlock-technology-ic",
                ("technology", Loc.GetString(technology.Name)),
                ("amount", cost),
                ("approver", approver));

            _chat.TrySendInGameICMessage(console, message, InGameICChatType.Speak, false);
        }
        // Arcane-End
    }
}
