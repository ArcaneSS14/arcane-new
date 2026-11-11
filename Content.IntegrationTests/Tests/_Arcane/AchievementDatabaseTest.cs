// Arcane-Start
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Content.Server.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class AchievementDatabaseTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = false, Dirty = true };

    [Test]
    public async Task SqliteConcurrentAndAmbiguousCommitBatchesAreAppliedOnce()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var fault = new CommitAcknowledgementFailure();
        var options = new DbContextOptionsBuilder<SqliteServerDbContext>().UseSqlite(connection).AddInterceptors(fault).Options;
        var cfg = Server.ResolveDependency<IConfigurationManager>();
        var log = Server.ResolveDependency<ILogManager>().GetSawmill("db.ops");
        var db = new ServerDbSqlite(() => options, true, cfg, true, log,
            Server.ResolveDependency<ISerializationManager>(), false);
        try
        {
            await Verify(db, () => new SqliteServerDbContext(options), fault);
        }
        finally
        {
            db.Shutdown();
        }
    }

    [Test]
    public async Task PostgresConcurrentAndAmbiguousCommitBatchesAreAppliedOnce()
    {
        var connection = AchievementMigrationTest.FreshPostgres("ARCANE_ACHIEVEMENTS_POSTGRES");
        var fault = new CommitAcknowledgementFailure();
        var options = new DbContextOptionsBuilder<PostgresServerDbContext>().UseNpgsql(connection).AddInterceptors(fault).Options;
        var log = Server.ResolveDependency<ILogManager>();
        var db = new ServerDbPostgres(options, connection, Server.ResolveDependency<IConfigurationManager>(),
            log.GetSawmill("db.ops"), log.GetSawmill("db.notify"), Server.ResolveDependency<ISerializationManager>());
        try
        {
            await Verify(db, () => new PostgresServerDbContext(options), fault);
        }
        finally
        {
            db.Shutdown();
        }
    }

    private static async Task Verify(ServerDbBase db, Func<ServerDbContext> contextFactory, CommitAcknowledgementFailure fault)
    {
        var user = new NetUserId(Guid.NewGuid());
        var otherUser = new NetUserId(Guid.NewGuid());
        await db.UpdatePlayerRecord(user, "Achievement test player", IPAddress.Loopback, null);
        await db.UpdatePlayerRecord(otherUser, "Another achievement player", IPAddress.Loopback, null);
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            db.AddAchievementProgressAsync(user, "DatabaseAward", Guid.NewGuid(), 1, null)));
        var progress = await db.GetAchievementProgressAsync(user, default);
        Assert.That(progress.Single().Progress, Is.EqualTo(8));

        var batch = Guid.NewGuid();
        var firstUnlock = new DateTime(2026, 11, 11, 1, 0, 0, DateTimeKind.Utc);
        await Task.WhenAll(
            db.AddAchievementProgressAsync(user, "DatabaseAward", batch, 3, firstUnlock),
            db.AddAchievementProgressAsync(user, "DatabaseAward", batch, 3, firstUnlock));
        await db.AddAchievementProgressAsync(user, "DatabaseAward", batch, 3, firstUnlock.AddDays(1));
        await db.AddAchievementProgressAsync(user, "DatabaseAward", Guid.NewGuid(), 0, firstUnlock.AddDays(2));
        progress = await db.GetAchievementProgressAsync(user, default);
        Assert.Multiple(() =>
        {
            Assert.That(progress, Has.Count.EqualTo(1));
            Assert.That(progress.Single().Progress, Is.EqualTo(11));
            Assert.That(progress.Single().UnlockedAt, Is.EqualTo(firstUnlock));
            Assert.That(progress.Single().UnlockedAt!.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
        });
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await db.AddAchievementProgressAsync(user, "AnotherAward", batch, 3, firstUnlock));
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await db.AddAchievementProgressAsync(otherUser, "DatabaseAward", batch, 3, firstUnlock));
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await db.AddAchievementProgressAsync(user, "DatabaseAward", Guid.NewGuid(), -1, null));
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await db.AddAchievementProgressAsync(user, "DatabaseAward", Guid.Empty, 1, null));
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await db.AddAchievementProgressAsync(user, new string('x', 129), Guid.NewGuid(), 1, null));

        await db.AcknowledgeAchievementBatchAsync(otherUser, batch);
        await db.AddAchievementProgressAsync(user, "DatabaseAward", batch, 3, firstUnlock);
        Assert.That((await db.GetAchievementProgressAsync(user, default)).Single().Progress, Is.EqualTo(11));
        await db.AcknowledgeAchievementBatchAsync(user, batch);
        await using (var context = contextFactory())
            Assert.That(await context.AchievementProgressBatches.AnyAsync(b => b.Id == batch), Is.False);

        // Commit succeeded in the provider, but the caller lost its acknowledgement.
        var uncertainBatch = Guid.NewGuid();
        fault.FailNextCommit();
        Assert.ThrowsAsync<IOException>(async () =>
            await db.AddAchievementProgressAsync(user, "UncertainAward", uncertainBatch, 5, null));
        Assert.That((await db.GetAchievementProgressAsync(user, default)).Single(p => p.AchievementId == "UncertainAward").Progress,
            Is.EqualTo(5), "The first transaction did commit before the injected failure.");
        await db.AddAchievementProgressAsync(user, "UncertainAward", uncertainBatch, 5, null);
        Assert.That((await db.GetAchievementProgressAsync(user, default)).Single(p => p.AchievementId == "UncertainAward").Progress,
            Is.EqualTo(5), "Retrying an unknown commit must not add the immutable batch again.");
        var stats = await db.GetAchievementStatsAsync(default);
        Assert.That(stats.Players, Is.EqualTo(1));
        Assert.That(stats.Unlocks["DatabaseAward"], Is.EqualTo(1));
    }

    private sealed class CommitAcknowledgementFailure : DbTransactionInterceptor
    {
        private int _pending;
        public void FailNextCommit() => Interlocked.Exchange(ref _pending, 1);

        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            return Interlocked.Exchange(ref _pending, 0) == 1
                ? Task.FromException(new IOException("Simulated lost commit acknowledgement."))
                : Task.CompletedTask;
        }
    }
}
// Arcane-End
