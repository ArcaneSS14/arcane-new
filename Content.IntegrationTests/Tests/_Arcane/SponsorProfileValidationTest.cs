// Arcane-Start: changing permissions must not destroy saved character choices
using System.Linq;
using System.Reflection;
using Content.Arcane.Server.DiscordRoles;
using Content.Arcane.Shared.DiscordRoles;
using Content.IntegrationTests.Fixtures;
using Content.Server.GameTicking;
using Content.Server.Preferences.Managers;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Robust.Shared.Enums;

namespace Content.IntegrationTests.Tests._Arcane;

[TestFixture]
public sealed class SponsorProfileValidationTest : GameTest
{
    [Test]
    public async Task AppearanceValidationDoesNotMutateSavedMarkings()
    {
        await Server.WaitAssertion(() =>
        {
            var appearance = HumanoidCharacterAppearance.DefaultWithSpecies("Human", Sex.Male);
            var original = new List<Marking> { new("NotARealMarking", Array.Empty<Color>()) };
            appearance.Markings["Head"] = new() { [HumanoidVisualLayers.Hair] = original };
            var validated = HumanoidCharacterAppearance.EnsureValid(appearance, "Human", Sex.Male);
            Assert.That(original, Has.Count.EqualTo(1));
            Assert.That(appearance.Markings["Head"][HumanoidVisualLayers.Hair], Is.SameAs(original));
            Assert.That(validated.Markings.Values.SelectMany(group => group.Values).SelectMany(markings => markings)
                .Any(marking => marking.MarkingId == "NotARealMarking"), Is.False);
        });
    }

    [Test]
    public async Task SpawnRechecksRoleWithoutLosingSavedLoadout()
    {
        await Server.WaitAssertion(() =>
        {
            var roles = Server.ResolveDependency<DiscordRoleManager>();
            var preferences = Server.ResolveDependency<IServerPreferencesManager>();
            var session = ServerSession!;
            var saved = preferences.GetPreferences(session.UserId).SelectedCharacter;
            var field = typeof(HumanoidCharacterProfile).GetField("_loadouts", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var loadouts = (Dictionary<string, RoleLoadout>) field.GetValue(saved)!;
            var hadOld = loadouts.TryGetValue("JobBartender", out var old);
            var selection = new RoleLoadout("JobBartender");
            selection.SelectedLoadouts["BartenderJumpsuit"] = [new Loadout { Prototype = "BartenderPatron" }];
            saved.SetLoadout(selection);

            bool HasPatron(HumanoidCharacterProfile profile) =>
                profile.Loadouts.TryGetValue("JobBartender", out var loadout)
                && loadout.SelectedLoadouts.TryGetValue("BartenderJumpsuit", out var items)
                && items.Any(item => item.Prototype == "BartenderPatron");

            try
            {
                Assert.That(roles.HasRole(session, DiscordRole.SponsorTier2), Is.False);
                roles.SetRoleOverride(session.UserId, DiscordRole.SponsorTier2, true);
                var ticker = SEntMan.System<ServerGameTicker>();
                Assert.That(HasPatron(ticker.GetPlayerProfile(session)), Is.True);
                roles.SetRoleOverride(session.UserId, DiscordRole.SponsorTier2, false);
                Assert.That(HasPatron(ticker.GetPlayerProfile(session)), Is.False);
                Assert.That(HasPatron(saved), Is.True, "Revalidation must preserve the original selection.");
                roles.SetRoleOverride(session.UserId, DiscordRole.SponsorTier2, true);
                Assert.That(HasPatron(ticker.GetPlayerProfile(session)), Is.True);
            }
            finally
            {
                roles.SetRoleOverride(session.UserId, DiscordRole.SponsorTier2, false);
                if (hadOld)
                    loadouts["JobBartender"] = old!;
                else
                    loadouts.Remove("JobBartender");
            }
        });
    }
}
// Arcane-End
