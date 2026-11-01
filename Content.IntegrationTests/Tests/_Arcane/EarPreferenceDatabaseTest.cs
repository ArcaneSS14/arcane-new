using System.Linq;
using Content.Server.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class EarPreferenceDatabaseTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = false, Dirty = true };

    [Test]
    public async Task SqliteUpgradeAndDowngradePreserveExistingCharacter()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new SqliteServerDbContext(new DbContextOptionsBuilder<SqliteServerDbContext>().UseSqlite(connection).Options);
        await Verify(context);
    }

    [Test]
    public async Task PostgresUpgradeAndDowngradePreserveExistingCharacter()
    {
        var connection = Environment.GetEnvironmentVariable("ARCANE_EARS_TEST_POSTGRES");
        if (string.IsNullOrWhiteSpace(connection))
            Assert.Ignore("Set ARCANE_EARS_TEST_POSTGRES to a fresh local arcane_goob_ears_test_* database on port55432.");
        var target = new NpgsqlConnectionStringBuilder(connection);
        Assert.That(target.Host, Is.EqualTo("127.0.0.1"));
        Assert.That(target.Port, Is.EqualTo(55432));
        Assert.That(target.Database, Does.StartWith("arcane_goob_ears_test_"));
        await using var context = new PostgresServerDbContext(new DbContextOptionsBuilder<PostgresServerDbContext>().UseNpgsql(connection).Options);
        Assert.That(await context.Database.GetAppliedMigrationsAsync(), Is.Empty);
        await Verify(context);
    }

    private static async Task Verify(ServerDbContext context)
    {
        Assert.That(context.Database.HasPendingModelChanges(), Is.False);
        var migrations = context.Database.GetMigrations().ToArray();
        var own = migrations.Single(migration => migration.EndsWith("_ArcaneEarsAboveHair"));
        var previous = migrations[Array.IndexOf(migrations, own) - 1];
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(previous);
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            var preference = new Preference { UserId = Guid.NewGuid(), SelectedCharacterSlot = 0, AdminOOCColor = "#FFFFFF" };
            context.Preference.Add(preference);
            await context.SaveChangesAsync();
            // Insert with the previous schema, before the new column exists.
            await context.Database.ExecuteSqlInterpolatedAsync($$"""
                INSERT INTO profile (preference_id, slot, char_name, flavor_text, age, scale, sex, gender, species,
                    bark_voice, knowledge_mastery, hair_name, hair_color, facial_hair_name, facial_hair_color,
                    eye_color, skin_color, spawn_priority, pref_unavailable)
                VALUES ({{preference.Id}}, 0, 'Ear preference migration', '', 30, 1.05, 'Male', 'Male', 'Vulpkanin',
                    'Default', '{}', '', '#000000', '', '#000000', '#000000', '#FFFFFF', 0, 0)
                """);
            await transaction.CommitAsync();
        }
        await migrator.MigrateAsync(own);
        var profile = await context.Profile.SingleAsync();
        Assert.That(profile.EarsAboveHair, Is.False, "Existing characters retain the source default.");
        profile.EarsAboveHair = true;
        await context.SaveChangesAsync();
        Assert.That(await context.Profile.AsNoTracking().Select(character => character.EarsAboveHair).SingleAsync(), Is.True);
        context.ChangeTracker.Clear();
        await migrator.MigrateAsync(previous);
        var preserved = await context.Profile.AsNoTracking().Select(character => new {character.CharacterName, character.Scale}).SingleAsync();
        Assert.That(preserved.CharacterName, Is.EqualTo("Ear preference migration"));
        Assert.That(preserved.Scale, Is.EqualTo(1.05f).Within(0.0001f));
        await migrator.MigrateAsync(own);
        Assert.That(await context.Profile.AsNoTracking().Select(character => character.EarsAboveHair).SingleAsync(), Is.False);
    }
}
