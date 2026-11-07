#nullable enable
using System.Linq;
using System.Net;
using Content.IntegrationTests.Fixtures;
using Content.Server.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class PollSeenDatabaseTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = false, Dirty = true };

    [Test]
    public async Task DuplicateAndUnavailableViewsStaySeparateFromVotes()
    {
        var db = Server.ResolveDependency<IServerDbManager>();
        var user = new NetUserId(Guid.NewGuid());
        await db.UpdatePlayerRecordAsync(user, "Poll viewer", IPAddress.Loopback, null);
        var poll = NewPoll();
        var id = await db.CreatePollAsync(poll);
        var results = await Task.WhenAll(db.MarkPollSeenAsync(id, user), db.MarkPollSeenAsync(id, user));
        Assert.That(results.Count(added => added), Is.EqualTo(1));
        Assert.That(await db.GetPollSeenCountAsync(id), Is.EqualTo(1));
        Assert.That(await db.GetPlayerVotesAsync(id, user), Is.Empty);
        Assert.That(await db.GetSeenPollIdsAsync(user), Does.Contain(id));
        Assert.That(await db.MarkPollSeenAsync(int.MaxValue, user), Is.False);
        Assert.That(await db.MarkPollSeenAsync(id, new NetUserId(Guid.NewGuid())), Is.False);
        foreach (var unavailable in new[] { "closed", "expired", "future" })
        {
            var other = NewPoll();
            if (unavailable == "closed") other.Active = false;
            if (unavailable == "expired") other.EndTime = DateTime.UtcNow.AddMinutes(-1);
            if (unavailable == "future") other.StartTime = DateTime.UtcNow.AddDays(1);
            var otherId = await db.CreatePollAsync(other);
            Assert.That(await db.MarkPollSeenAsync(otherId, user), Is.False, unavailable);
            Assert.That(await db.GetPollSeenCountAsync(otherId), Is.Zero);
        }
        Assert.That(await db.AddPollVoteAsync(id, poll.Options[0].Id, user), Is.True);
        Assert.That(await db.GetPlayerVotesAsync(id, user), Has.Count.EqualTo(1));
        Assert.That(await db.GetPollSeenCountAsync(id), Is.EqualTo(1));
    }

    [Test]
    public async Task SqliteUpgradeAndDowngradePreserveExistingPollsAndVotes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SqliteServerDbContext>().UseSqlite(connection).Options;
        await using var context = new SqliteServerDbContext(options);
        await VerifyUpgradeAndDowngrade(context);
    }

    [Test]
    public async Task PostgresUpgradeAndDowngradePreserveExistingPollsAndVotes()
    {
        var connection = Environment.GetEnvironmentVariable("ARCANE_TEST_POSTGRES");
        if (string.IsNullOrWhiteSpace(connection))
            Assert.Ignore("Set ARCANE_TEST_POSTGRES to a fresh local arcane_goob_poll_test_* database on port 55432.");
        var target = new NpgsqlConnectionStringBuilder(connection);
        Assert.That(target.Host, Is.EqualTo("127.0.0.1"));
        Assert.That(target.Port, Is.EqualTo(55432));
        Assert.That(target.Database, Does.StartWith("arcane_goob_poll_test_"));
        await using var context = new PostgresServerDbContext(new DbContextOptionsBuilder<PostgresServerDbContext>()
            .UseNpgsql(connection).Options);
        Assert.That(await context.Database.GetAppliedMigrationsAsync(), Is.Empty, "Use a fresh test database.");
        await VerifyUpgradeAndDowngrade(context);
    }

    private static async Task VerifyUpgradeAndDowngrade(ServerDbContext context)
    {
        Assert.That(context.Database.HasPendingModelChanges(), Is.False);
        var migrations = context.Database.GetMigrations().ToArray();
        var own = migrations.Single(m => m.EndsWith("_ArcanePollSeen"));
        var previous = migrations[Array.IndexOf(migrations, own) - 1];
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(previous);
        var player = new Player
        {
            UserId = Guid.NewGuid(), FirstSeenTime = DateTime.UtcNow, LastSeenTime = DateTime.UtcNow,
            LastSeenUserName = "Existing viewer", LastSeenAddress = IPAddress.Loopback
        };
        var poll = NewPoll();
        context.Player.Add(player);
        context.Polls.Add(poll);
        await context.SaveChangesAsync();
        context.PollVotes.Add(new PollVote
        {
            Poll = poll, PollOption = poll.Options[0], PlayerUserId = player.UserId, VotedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        await migrator.MigrateAsync(own);
        context.PollSeen.Add(new PollSeen { PollId = poll.Id, PlayerUserId = player.UserId, SeenAt = DateTime.UtcNow });
        await context.SaveChangesAsync();
        context.PollSeen.Add(new PollSeen { PollId = poll.Id, PlayerUserId = player.UserId, SeenAt = DateTime.UtcNow });
        Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
        context.ChangeTracker.Clear();
        Assert.That(await context.PollSeen.CountAsync(), Is.EqualTo(1));
        await migrator.MigrateAsync(previous);
        Assert.That(await context.Polls.CountAsync(), Is.EqualTo(1));
        Assert.That(await context.PollVotes.CountAsync(), Is.EqualTo(1));
        await migrator.MigrateAsync(own);
        Assert.That(await context.PollSeen.CountAsync(), Is.Zero);
        Assert.That(await context.PollVotes.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public void PostgresMigrationSqlChangesOnlyViewHistory()
    {
        using var context = new PostgresServerDbContext(new DbContextOptionsBuilder<PostgresServerDbContext>()
            .UseNpgsql("Host=localhost;Database=poll_migration_validation").Options);
        Assert.That(context.Database.HasPendingModelChanges(), Is.False);
        var migrations = context.Database.GetMigrations().ToArray();
        var own = migrations.Single(m => m.EndsWith("_ArcanePollSeen"));
        var previous = migrations[Array.IndexOf(migrations, own) - 1];
        var migrator = context.GetService<IMigrator>();
        var up = migrator.GenerateScript(previous, own);
        var down = migrator.GenerateScript(own, previous);
        Assert.That(up, Does.Contain("CREATE TABLE poll_seen").And.Contain("CREATE UNIQUE INDEX"));
        Assert.That(up, Does.Contain("REFERENCES player (user_id)").And.Contain("REFERENCES polls (polls_id)"));
        Assert.That(up, Does.Not.Contain("DROP TABLE"));
        Assert.That(down, Does.Contain("DROP TABLE poll_seen"));
        Assert.That(down, Does.Not.Contain("DROP TABLE polls").And.Not.Contain("DROP TABLE poll_votes"));
    }

    private static Poll NewPoll() => new()
    {
        Title = "View history test", Description = "Independent of voting",
        StartTime = DateTime.UtcNow.AddMinutes(-2), CreatedAt = DateTime.UtcNow, Active = true,
        Options = [new PollOption { OptionText = "First", DisplayOrder = 0 }, new PollOption { OptionText = "Second", DisplayOrder = 1 }]
    };
}
