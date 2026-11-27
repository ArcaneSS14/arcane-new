// Arcane-Start: capture and restore character customization around a disguise
using Content.Arcane.Common.Preferences;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Trauma.Server.Abductor;

namespace Content.IntegrationTests.Tests._Arcane;

[TestFixture]
public sealed class CustomizationDisguiseTest : GameTest
{
    [Test]
    public async Task DisguiseRestoresAnIndependentCustomizationSnapshot()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var mob = SSpawnAtPosition("MobHuman", map.GridCoords);
            var custom = new CharacterCustomization
            {
                CustomSpeciesName = "Cyborg",
                HairGradientEnabled = true,
                HairGradientColors = new() { Color.Red, Color.Blue },
                HairGradientStyle = HairGradientStyle.Split,
                HairGradientOffset = 0.75f,
            };
            var humanoids = Server.System<HumanoidProfileSystem>();
            humanoids.ApplyProfileTo(mob, HumanoidCharacterProfile.DefaultWithSpecies("Human").WithCustomization(custom));
            var captured = humanoids.CreateProfile(mob);
            Assert.That(captured, Is.Not.Null);
            custom.HairGradientColors[0] = Color.Green;
            Assert.That(captured!.Customization.HairGradientColors[0], Is.EqualTo(Color.Red), "The captured colors must not alias live state.");
            custom.HairGradientColors[0] = Color.Red;

            var disguise = Server.System<AbductorVestDisguiseSystem>();
            disguise.ApplyDisguise(mob, HumanoidCharacterProfile.DefaultWithSpecies("Human").WithName("Temporary disguise"));
            Assert.That(SEntMan.GetComponent<HumanoidProfileComponent>(mob).Customization.HairGradientEnabled, Is.False);
            disguise.RestoreAppearance(mob);
            var restored = SEntMan.GetComponent<HumanoidProfileComponent>(mob).Customization;
            Assert.That(restored.CustomSpeciesName, Is.EqualTo("Cyborg"));
            Assert.That(restored.HairGradientEnabled, Is.True);
            Assert.That(restored.HairGradientColors, Is.EqualTo(new[] { Color.Red, Color.Blue }));
            Assert.That(restored.HairGradientStyle, Is.EqualTo(HairGradientStyle.Split));
            Assert.That(restored.HairGradientOffset, Is.EqualTo(0.75f));
        });
    }
}
// Arcane-End
