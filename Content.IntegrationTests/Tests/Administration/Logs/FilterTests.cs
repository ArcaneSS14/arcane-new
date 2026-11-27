using Content.IntegrationTests.Fixtures;
using Content.Server.Administration.Logs;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Robust.Shared.GameObjects;
// Arcane-Start
using Content.Arcane.Common.Administration;
using Content.Server.Database;
using System.Linq;
using System.Text.Json;
// Arcane-End

namespace Content.IntegrationTests.Tests.Administration.Logs;

[TestFixture]
[TestOf(typeof(AdminLogSystem))]
public sealed class FilterTests : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        AdminLogsEnabled = true,
        DummyTicker = false,
        Connected = true
    };

    [Test]
    [TestCase(DateOrder.Ascending)]
    [TestCase(DateOrder.Descending)]
    public async Task Date(DateOrder order)
    {
        var pair = Pair;
        var server = pair.Server;

        var sEntities = server.ResolveDependency<IEntityManager>();

        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();

        var commonGuid = Guid.NewGuid();
        var firstGuid = Guid.NewGuid();
        var secondGuid = Guid.NewGuid();
        var testMap = await pair.CreateTestMap();
        var coordinates = testMap.GridCoords;

        await server.WaitPost(() =>
        {
            var entity = sEntities.SpawnEntity(null, coordinates);

            sAdminLogSystem.Add(LogType.Unknown, $"{entity:Entity} test log: {commonGuid} {firstGuid}");
        });

        await Task.Delay(2000);

        await server.WaitPost(() =>
        {
            var entity = sEntities.SpawnEntity(null, coordinates);

            sAdminLogSystem.Add(LogType.Unknown, $"{entity:Entity} test log: {commonGuid} {secondGuid}");
        });

        await PoolManager.WaitUntil(server, async () =>
        {
            var commonGuidStr = commonGuid.ToString();

            string firstGuidStr;
            string secondGuidStr;

            switch (order)
            {
                case DateOrder.Ascending:
                    // Oldest first
                    firstGuidStr = firstGuid.ToString();
                    secondGuidStr = secondGuid.ToString();
                    break;
                case DateOrder.Descending:
                    // Newest first
                    firstGuidStr = secondGuid.ToString();
                    secondGuidStr = firstGuid.ToString();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(order), order, null);
            }

            var firstFound = false;
            var secondFound = false;

            var both = await sAdminLogSystem.CurrentRoundLogs(new LogFilter
            {
                Search = commonGuidStr,
                DateOrder = order
            });

            foreach (var log in both)
            {
                if (!log.Message.Contains(commonGuidStr))
                {
                    continue;
                }

                if (!firstFound)
                {
                    Assert.That(log.Message, Does.Contain(firstGuidStr));
                    firstFound = true;
                    continue;
                }

                Assert.That(log.Message, Does.Contain(secondGuidStr));
                secondFound = true;
                break;
            }

            return firstFound && secondFound;
        });
    }
    // Arcane-Start
    [Test]
    [TestCase(DateOrder.Ascending)]
    [TestCase(DateOrder.Descending)]
    public async Task CurrentRoundFilteredCursorPagesKeepOrder(DateOrder order)
    {
        var logs = Server.ResolveDependency<IAdminLogManager>();
        var marker = Guid.NewGuid().ToString();
        await Server.WaitPost(() =>
        {
            logs.Add(LogType.Unknown, $"{marker} first");
            logs.Add(LogType.Ahelp, $"{marker} excluded");
            logs.Add(LogType.Unknown, $"{marker} second");
            logs.Add(LogType.Unknown, $"{marker} third");
        });

        var filter = new LogFilter
        {
            Search = marker,
            Types = new() { LogType.Unknown },
            DateOrder = order,
        };
        var all = new List<Content.Shared.Administration.Logs.SharedAdminLog>();
        await PoolManager.WaitUntil(Server, async () =>
        {
            all = await logs.CurrentRoundLogs(filter);
            return all.Count == 3;
        });

        var expected = order == DateOrder.Ascending
            ? all.OrderBy(log => log.Date).ThenBy(log => log.RoundId).ThenBy(log => log.Id).ToArray()
            : all.OrderByDescending(log => log.Date).ThenByDescending(log => log.RoundId).ThenByDescending(log => log.Id).ToArray();
        filter.Limit = 1;
        var actual = new List<Content.Shared.Administration.Logs.SharedAdminLog>();
        for (var i = 0; i < expected.Length; i++)
        {
            var page = await logs.CurrentRoundLogs(filter);
            Assert.That(page, Has.Count.EqualTo(1));
            actual.Add(page[0]);
            filter.Cursor = new AdminLogCursor(page[0].Date, page[0].RoundId, page[0].Id);
        }
        Assert.That(actual.Select(log => log.Id), Is.EqualTo(expected.Select(log => log.Id)));
        Assert.That(await logs.CurrentRoundLogs(filter), Is.Empty);
    }

    [Test]
    public async Task HistoryCursorKeepsLogsWithTheSameDateAcrossRounds()
    {
        var db = Server.ResolveDependency<IServerDbManager>();
        var dbServer = await db.AddOrGetServer($"history-cursor-{Guid.NewGuid()}");
        var firstRound = await db.AddNewRound(dbServer);
        var secondRound = await db.AddNewRound(dbServer);
        var date = DateTime.UtcNow.AddDays(-1);
        var marker = Guid.NewGuid().ToString();
        using var json = JsonDocument.Parse("{}");
        var logs = new List<AdminLog>();
        foreach (var round in new[] { firstRound, secondRound })
        {
            for (var id = 1; id <= 2; id++)
            {
                logs.Add(new AdminLog
                {
                    RoundId = round,
                    Id = id,
                    Date = date,
                    Type = LogType.Ahelp,
                    Impact = LogImpact.Low,
                    Message = marker,
                    Json = json,
                    Players = new(),
                });
            }
        }
        await db.AddAdminLogs(logs);

        var filter = new LogFilter
        {
            Search = marker,
            Types = new() { LogType.Ahelp },
            DateOrder = DateOrder.Ascending,
            Limit = 2,
        };
        var first = new List<Content.Shared.Administration.Logs.SharedAdminLog>();
        await foreach (var log in db.GetAdminLogs(filter))
            first.Add(log);
        Assert.That(first.Select(log => (log.RoundId, log.Id)),
            Is.EqualTo(new[] { (firstRound, 1), (firstRound, 2) }));

        filter.Cursor = new AdminLogCursor(date, firstRound, 2);
        var second = new List<Content.Shared.Administration.Logs.SharedAdminLog>();
        await foreach (var log in db.GetAdminLogs(filter))
            second.Add(log);
        Assert.That(second.Select(log => (log.RoundId, log.Id)),
            Is.EqualTo(new[] { (secondRound, 1), (secondRound, 2) }));
    }
    // Arcane-End
}
