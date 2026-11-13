// Arcane-Start
using System.Data.Common;
using System.Linq;
using System.Net;
using Content.Server.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using NUnit.Framework;

namespace Content.IntegrationTests.Tests._Arcane;

[TestFixture]
public sealed class AchievementMigrationTest
{
    [Test]
    public async Task SqliteUpgradeRollbackAndReapplyPreserveExistingPlayer()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new SqliteServerDbContext(new DbContextOptionsBuilder<SqliteServerDbContext>()
            .UseSqlite(connection).Options);
        await Verify(context);
    }

    [Test]
    public async Task PostgresUpgradeRollbackAndReapplyPreserveExistingPlayer()
    {
        var connection = FreshPostgres("ARCANE_ACHIEVEMENTS_SCHEMA_POSTGRES");
        await using var context = new PostgresServerDbContext(new DbContextOptionsBuilder<PostgresServerDbContext>()
            .UseNpgsql(connection).Options);
        Assert.That(await context.Database.GetAppliedMigrationsAsync(), Is.Empty);
        await Verify(context);
    }

    internal static string FreshPostgres(string variable)
    {
        var connection = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(connection))
            Assert.Ignore($"Set {variable} to a fresh local arcane_goob_achievements_test_* database on port 55432.");
        var target = new NpgsqlConnectionStringBuilder(connection!);
        Assert.Multiple(() =>
        {
            Assert.That(target.Host, Is.EqualTo("127.0.0.1"));
            Assert.That(target.Port, Is.EqualTo(55432));
            Assert.That(target.Database, Does.StartWith("arcane_goob_achievements_test_"));
        });
        return connection!;
    }

    private static async Task Verify(ServerDbContext context)
    {
        Assert.That(context.Database.HasPendingModelChanges(), Is.False);
        var migrations = context.Database.GetMigrations().ToArray();
        var own = migrations.Single(m => m.EndsWith("_ArcaneAchievements"));
        var previous = migrations[Array.IndexOf(migrations, own) - 1];
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(previous);
        var player = new Player
        {
            UserId = Guid.NewGuid(), FirstSeenTime = DateTime.UtcNow, LastSeenTime = DateTime.UtcNow,
            LastSeenUserName = "Achievement migration player", LastSeenAddress = IPAddress.Loopback,
        };
        context.Player.Add(player);
        await context.SaveChangesAsync();
        await migrator.MigrateAsync(own);
        var unlockedAt = new DateTime(2026, 11, 11, 0, 0, 0, DateTimeKind.Utc);
        context.AchievementProgress.Add(new AchievementProgress
        {
            PlayerUserId = player.UserId, AchievementId = "MigrationAward", Progress = 7, UnlockedAt = unlockedAt,
        });
        context.AchievementProgressBatches.Add(new AchievementProgressBatch
        {
            Id = Guid.NewGuid(), PlayerUserId = player.UserId, AchievementId = "MigrationAward", Delta = 7,
            MarksUnlocked = true,
        });
        await context.SaveChangesAsync();
        context.AchievementProgress.Add(new AchievementProgress
        {
            PlayerUserId = player.UserId, AchievementId = "MigrationAward", Progress = 99,
        });
        Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
        context.ChangeTracker.Clear();
        Assert.That(await context.AchievementProgress.CountAsync(), Is.EqualTo(1));
        Assert.That((await context.AchievementProgress.SingleAsync()).UnlockedAt, Is.EqualTo(unlockedAt));
        context.ChangeTracker.Clear();

        await migrator.MigrateAsync(previous);
        Assert.That(await context.Player.CountAsync(p => p.UserId == player.UserId), Is.EqualTo(1));
        Assert.CatchAsync<DbException>(async () => await context.AchievementProgress.CountAsync());
        Assert.CatchAsync<DbException>(async () => await context.AchievementProgressBatches.CountAsync());
        await migrator.MigrateAsync(own);
        Assert.That(await context.AchievementProgress.CountAsync(), Is.Zero);
        Assert.That(await context.AchievementProgressBatches.CountAsync(), Is.Zero);
        Assert.That((await context.Player.SingleAsync()).LastSeenUserName, Is.EqualTo(player.LastSeenUserName));

        context.AchievementProgress.Add(new AchievementProgress
        {
            PlayerUserId = player.UserId, AchievementId = "CascadeAward", Progress = 1,
        });
        context.AchievementProgressBatches.Add(new AchievementProgressBatch
        {
            Id = Guid.NewGuid(), PlayerUserId = player.UserId, AchievementId = "CascadeAward", Delta = 1,
        });
        await context.SaveChangesAsync();
        await context.Player.Where(p => p.UserId == player.UserId).ExecuteDeleteAsync();
        Assert.That(await context.AchievementProgress.CountAsync(), Is.Zero);
        Assert.That(await context.AchievementProgressBatches.CountAsync(), Is.Zero);
    }
}
// Arcane-End
