using System.Linq;
using System.Threading;
using Content.Arcane.Common.Administration;
using Content.Arcane.Common.CCVar;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.Players.RateLimiting;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Players.RateLimiting;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Arcane.Server.Administration;

public sealed partial class BwoinkHistorySystem : EntitySystem
{
    private const string HistoryRateLimitKey = "AdminHelpHistory";
    private const int PageSize = 200;

    [Dependency] private IAdminManager _admins = default!;
    [Dependency] private IAdminLogManager _logs = default!;
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private PlayerRateLimitManager _rateLimit = default!;

    private readonly SemaphoreSlim _requests = new(2);
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("ahelp.history");
        SubscribeNetworkEvent<BwoinkHistoryRequest>(OnHistoryRequest);
        SubscribeLocalEvent<BwoinkHistoryEntryEvent>(OnHistoryEntry);
        _rateLimit.Register(HistoryRateLimitKey, new RateLimitRegistration(
            ArcaneCVars.AhelpHistoryRateLimitPeriod, ArcaneCVars.AhelpHistoryRateLimitCount, null));
    }

    private void OnHistoryEntry(BwoinkHistoryEntryEvent ev)
    {
        _logs.Add(ev.AdminOnly ? LogType.AhelpAdminOnly : LogType.Ahelp, LogImpact.Low,
            $"{new HistoryPlayer(ev.Channel)}{ev.Text}");
    }

    private async void OnHistoryRequest(BwoinkHistoryRequest request, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        var isAdmin = HasHistoryPermission(session);
        if (!isAdmin && request.Channel != session.UserId)
            return;

        if (_rateLimit.CountAction(session, HistoryRateLimitKey) != RateLimitStatus.Allowed || !_requests.Wait(0))
            return;

        try
        {
            var filter = new LogFilter
            {
                Types = isAdmin
                    ? new HashSet<LogType> { LogType.Ahelp, LogType.AhelpAdminOnly }
                    : new HashSet<LogType> { LogType.Ahelp },
                AnyPlayers = [request.Channel.UserId],
                IncludePlayers = true,
                After = DateTime.UtcNow.AddMonths(-2),
                DateOrder = DateOrder.Ascending,
                Cursor = request.Cursor,
                Limit = PageSize + 1,
            };
            var logs = new List<SharedAdminLog>();
            await foreach (var log in _db.GetAdminLogs(filter))
                logs.Add(log);

            var currentFilter = new LogFilter
            {
                Types = filter.Types,
                AnyPlayers = filter.AnyPlayers,
                IncludePlayers = true,
                After = filter.After,
                DateOrder = DateOrder.Ascending,
                Cursor = request.Cursor,
                Limit = PageSize + 1,
            };
            var current = await _logs.CurrentRoundLogs(currentFilter);
            var saved = new HashSet<(int Round, int Id)>(logs.Select(log => (log.RoundId, log.Id)));
            foreach (var log in current)
            {
                if (saved.Add((log.RoundId, log.Id)))
                    logs.Add(log);
            }

            // A disconnect or privilege revocation during the query invalidates this request.
            if (!_players.TryGetSessionById(session.UserId, out var currentSession) ||
                currentSession != session || isAdmin && !HasHistoryPermission(session))
                return;

            var page = logs.OrderBy(log => log.Date).ThenBy(log => log.RoundId).ThenBy(log => log.Id)
                .Take(PageSize).ToList();
            var messages = page.Select(log => new BwoinkHistoryMessage(log.Date, log.Message,
                log.Type == LogType.AhelpAdminOnly, log.RoundId)).ToList();
            AdminLogCursor? next = page.Count == 0
                ? null
                : new AdminLogCursor(page[^1].Date, page[^1].RoundId, page[^1].Id);
            RaiseNetworkEvent(new BwoinkHistoryResponse(request.Channel, messages, next,
                logs.Count > PageSize, request.Cursor), session);
        }
        catch (Exception e)
        {
            _sawmill.Warning($"Could not load AHelp history for {request.Channel}: {e}");
        }
        finally
        {
            _requests.Release();
        }
    }

    private bool HasHistoryPermission(ICommonSession session)
    {
        return _admins.GetAdminData(session)?.HasFlag(AdminFlags.Adminhelp) ?? false;
    }

    private readonly struct HistoryPlayer(NetUserId userId) : IAdminLogsPlayerValue
    {
        public IEnumerable<NetUserId> Players => [userId];
        public override string ToString() => string.Empty;
    }
}
