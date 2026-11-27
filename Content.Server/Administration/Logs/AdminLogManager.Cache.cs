using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Database;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Prometheus;

namespace Content.Server.Administration.Logs;

public sealed partial class AdminLogManager
{
    private const int MaxRoundsCached = 3;
    private const int LogListInitialSize = 30_000;

    private readonly int _logTypes = Enum.GetValues<LogType>().Length;

    // TODO ADMIN LOGS make this thread safe or remove thread safety from the main partial class
    private readonly Dictionary<int, List<SharedAdminLog>> _roundsLogCache = new(MaxRoundsCached);
    private readonly Queue<int> _roundsLogCacheQueue = new();

    private static readonly Gauge CacheRoundCount = Metrics.CreateGauge(
        "admin_logs_cache_round_count",
        "How many rounds are in cache.");

    private static readonly Gauge CacheLogCount = Metrics.CreateGauge(
        "admin_logs_cache_log_count",
        "How many logs are in cache.");

    // TODO ADMIN LOGS cache previous {MaxRoundsCached} rounds on startup
    public void CacheNewRound()
    {
        List<SharedAdminLog>? list = null;

        _roundsLogCacheQueue.Enqueue(_currentRoundId);
        if (_roundsLogCacheQueue.Count > MaxRoundsCached)
        {
            var oldestRound = _roundsLogCacheQueue.Dequeue();
            if (_roundsLogCache.Remove(oldestRound, out var oldestList))
            {
                list = oldestList;
                list.Clear();
            }
        }

        list ??= new List<SharedAdminLog>(LogListInitialSize);

        _roundsLogCache.Add(_currentRoundId, list);
        CacheRoundCount.Set(_roundsLogCache.Count);
    }

    private void CacheLog(AdminLog log)
    {
        var players = log.Players.Select(player => player.PlayerUserId).ToArray();
        var record = new SharedAdminLog(log.Id, log.Type, log.Impact, log.Date, log.Message, players, log.RoundId); // Arcane-Edit: players > players, log.RoundId

        CacheLog(record);
    }

    private void CacheLog(SharedAdminLog log)
    {
        // TODO ADMIN LOGS remove redundant data and don't do a dictionary lookup per log
        var cache = _roundsLogCache[_currentRoundId];
        cache.Add(log);
        CacheLogCount.Set(cache.Count);
    }

    private void CacheLogs(IEnumerable<SharedAdminLog> logs)
    {
        var cache = _roundsLogCache[_currentRoundId];
        cache.AddRange(logs);
        CacheLogCount.Set(cache.Count);
    }

    private bool TryGetCache(int roundId, [NotNullWhen(true)] out List<SharedAdminLog>? cache)
    {
        return _roundsLogCache.TryGetValue(roundId, out cache);
    }

    private bool TrySearchCache(LogFilter? filter, [NotNullWhen(true)] out List<SharedAdminLog>? results)
    {
        if (filter?.Round == null || !TryGetCache(filter.Round.Value, out var cache))
        {
            results = null;
            return false;
        }

        // TODO ADMIN LOGS a better heuristic than linq spaghetti
        var query = cache.AsEnumerable();

        if (filter.Search != null)
        {
            query = query.Where(log => log.Message.Contains(filter.Search, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.Types != null && filter.Types.Count != _logTypes)
        {
            query = query.Where(log => filter.Types.Contains(log.Type));
        }

        if (filter.Impacts != null)
        {
            query = query.Where(log => filter.Impacts.Contains(log.Impact));
        }

        if (filter.Before != null)
        {
            query = query.Where(log => log.Date < filter.Before);
        }

        if (filter.After != null)
        {
            query = query.Where(log => log.Date > filter.After);
        }

        if (filter.IncludePlayers)
        {
            if (filter.AnyPlayers != null)
            {
                query = query.Where(log =>
                    filter.AnyPlayers.Any(filterPlayer => log.Players.Contains(filterPlayer)) ||
                    log.Players.Length == 0 && filter.IncludeNonPlayers);
            }

            if (filter.AllPlayers != null)
            {
                query = query.Where(log =>
                    filter.AllPlayers.All(filterPlayer => log.Players.Contains(filterPlayer)) ||
                    log.Players.Length == 0 && filter.IncludeNonPlayers);
            }
        }
        else
        {
            query = query.Where(log => log.Players.Length == 0);
        }

        // Arcane-Start
        if (filter.Cursor is { } cursor)
        {
            query = filter.DateOrder switch
            {
                DateOrder.Ascending => query.Where(log =>
                    log.Date > cursor.Date ||
                    log.Date == cursor.Date && log.RoundId > cursor.RoundId ||
                    log.Date == cursor.Date && log.RoundId == cursor.RoundId && log.Id > cursor.Id),
                DateOrder.Descending => query.Where(log =>
                    log.Date < cursor.Date ||
                    log.Date == cursor.Date && log.RoundId < cursor.RoundId ||
                    log.Date == cursor.Date && log.RoundId == cursor.RoundId && log.Id < cursor.Id),
                _ => throw new ArgumentOutOfRangeException(nameof(filter))
            };
        }
        // Arcane-End

        query = filter.DateOrder switch
        {
            // Arcane-Edit-Start: order by date, round and log ID
            DateOrder.Ascending => query.OrderBy(log => log.Date).ThenBy(log => log.RoundId).ThenBy(log => log.Id),
            DateOrder.Descending => query.OrderByDescending(log => log.Date).ThenByDescending(log => log.RoundId).ThenByDescending(log => log.Id),
            // Arcane-Edit-End
            _ => throw new ArgumentOutOfRangeException(nameof(filter),
                $"Unknown {nameof(DateOrder)} value {filter.DateOrder}")
        };

        if (filter.LogsSent != 0)
        {
            query = query.Skip(filter.LogsSent);
        }

        if (filter.Limit != null)
        {
            query = query.Take(filter.Limit.Value);
        }

        // TODO ADMIN LOGS array pool
        results = query.ToList();
        return true;
    }
}
