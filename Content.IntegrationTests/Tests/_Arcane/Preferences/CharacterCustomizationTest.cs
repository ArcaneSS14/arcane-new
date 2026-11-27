// Arcane-Start
using Content.Arcane.Common.Preferences;
using Content.IntegrationTests.Fixtures;
using Content.Server.Database;
using Content.Server.Preferences.Managers;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.IntegrationTests.Tests._Arcane.Preferences;

/// <summary>
/// The custom species name and the hair gradient survive the database and get validated.
/// </summary>
[TestFixture]
public sealed class CharacterCustomizationTest : GameTest
{
    private static readonly ProtoId<SpeciesPrototype> Reptilian = "Reptilian";

    private static CharacterCustomization Sample(string speciesName) => new()
    {
        CustomSpeciesName = speciesName,
        HairGradientEnabled = true,
        HairGradientColors = [Color.Red, Color.Blue],
        HairGradientStyle = HairGradientStyle.Split,
        HairGradientOffset = 0.75f,
    };

    private static HumanoidCharacterProfile Profile(string species, CharacterCustomization customization)
    {
        return new HumanoidCharacterProfile
        {
            Name = "Customized Person",
            Species = species,
            Age = 30,
            Appearance = new(Color.Azure, Color.Beige, new()),
        }.WithCustomization(customization);
    }

    [Test]
    public async Task CustomizationRoundTrip()
    {
        var server = Pair.Server;
        var cfg = server.ResolveDependency<IConfigurationManager>();
        var serialization = server.ResolveDependency<ISerializationManager>();
        var log = server.ResolveDependency<ILogManager>().GetSawmill("db.ops");
        var builder = new DbContextOptionsBuilder<SqliteServerDbContext>();
        var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        builder.UseSqlite(conn);
        var db = new ServerDbSqlite(() => builder.Options, true, cfg, true, log, serialization, false);

        var user = new NetUserId(new Guid("5d0c8c1e-1f6e-4f7c-9d9e-0c6b2f3a4b5c"));
        var profile = Profile("Human", Sample("Cyborg"));
        await db.InitPrefsAsync(user, profile);

        var preferences = (ServerPreferencesManager) server.ResolveDependency<IServerPreferencesManager>();
        var prefs = await db.GetPlayerPreferencesAsync(user);
        var loaded = preferences.ConvertProfiles(prefs!.Profiles.Find(p => p.Slot == 0));
        Assert.That(loaded.Customization, Is.EqualTo(profile.Customization));
    }

    [Test]
    public async Task CustomSpeciesNameNeedsTheSpecies()
    {
        var server = Pair.Server;
        await server.WaitAssertion(() =>
        {
            var prototypes = server.ResolveDependency<IPrototypeManager>();
            var cfg = server.ResolveDependency<IConfigurationManager>();
            foreach (var species in prototypes.EnumeratePrototypes<SpeciesPrototype>())
            {
                if (!species.RoundStart)
                    continue;

                var validated = Profile(species.ID, Sample("Cyborg")).Customization
                    .Validated(species.CustomName, 32, null);
                Assert.That(validated.CustomSpeciesName, Is.EqualTo(species.CustomName ? "Cyborg" : string.Empty), species.ID);
            }

            // A custom name cannot be another species' name while that is restricted.
            var reptilianName = Loc.GetString(prototypes.Index(Reptilian).Name);
            var human = Sample(reptilianName);
            var restricted = human.Validated(true, 32, [reptilianName]);
            Assert.That(restricted.CustomSpeciesName, Is.Empty);
            Assert.That(cfg, Is.Not.Null);
        });
    }
}
// Arcane-End
