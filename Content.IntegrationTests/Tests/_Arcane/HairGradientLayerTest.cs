// Arcane-Start: gradient color and displacement layer lifecycle
using Content.Arcane.Common.Preferences;
using Content.Client.Body;
using Content.Client.DisplacementMap;
using Content.Client.Humanoid;
using Content.IntegrationTests.Fixtures;
using Content.Shared.DisplacementMap;
using Content.Shared.Humanoid;
using System.Collections;
using System.Reflection;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Arcane;

[TestFixture]
public sealed class HairGradientLayerTest : GameTest
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task GradientPreservesColorAlphaAndRestoresExistingShader(bool displaced)
    {
        var map = await Pair.CreateTestMap();
        NetEntity mobNet = default;
        await Server.WaitPost(() => mobNet = SEntMan.GetNetEntity(SSpawnAtPosition("MobHuman", map.GridCoords)));
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            var mob = CEntMan.GetEntity(mobNet);
            var sprite = CEntMan.GetComponent<SpriteComponent>(mob);
            var profile = CEntMan.GetComponent<HumanoidProfileComponent>(mob);
            var originalCustomization = profile.Customization;
            var sprites = Client.System<SpriteSystem>();
            var gradients = Client.System<HairGradientSystem>();
            var tracked = (IDictionary) typeof(HairGradientSystem).GetField("_tracked", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(gradients)!;
            var displacement = Client.System<DisplacementMapSystem>();
            const string key = "review-gradient-test";
            var rsi = new SpriteSpecifier.Rsi(new ResPath("Mobs/Customization/human_hair.rsi"), "afro");
            var index = sprites.AddLayer((mob, sprite), rsi);
            sprites.LayerMapSet((mob, sprite), key, index);
            var baseColor = Color.Black.WithAlpha(0.4f);
            sprites.LayerSetColor((mob, sprite), index, baseColor);
            if (displaced)
            {
                ProtoId<DisplacementDataPrototype> dwarfism = "Dwarfism";
                var data = Client.ResolveDependency<IPrototypeManager>().Index(dwarfism).Displacement;
                Assert.That(displacement.TryAddDisplacement(data, (mob, sprite), index, key, out _), Is.True);
                Assert.That(sprites.LayerMapTryGet((mob, sprite), key, out index, false), Is.True);
            }

            var oldShader = ((SpriteComponent.Layer) sprite[index]).Shader;
            profile.Customization = new CharacterCustomization { HairGradientEnabled = true, HairGradientColors = [Color.Red, Color.Blue] };
            gradients.OnHairLayer(mob, key, rsi);
            var layer = (SpriteComponent.Layer) sprite[index];
            var gradientShader = layer.Shader;
            Assert.That(gradientShader, Is.Not.Null);
            Assert.That(gradientShader, Is.Not.SameAs(oldShader));
            Assert.That(layer.Color, Is.EqualTo(Color.White.WithAlpha(baseColor.A)));
            if (displaced)
                Assert.That(sprites.LayerMapTryGet((mob, sprite), $"{key}-displacement", out _, false), Is.True);

            profile.Customization = new CharacterCustomization();
            gradients.FrameUpdate(0f);
            Assert.That(layer.Color, Is.EqualTo(baseColor));
            Assert.That(layer.Shader, Is.SameAs(oldShader));
            var trackingEntry = tracked[mob]!;
            var shaders = (IDictionary) trackingEntry.GetType().GetField("Shaders")!.GetValue(trackingEntry)!;
            Assert.That(shaders.Contains(key), Is.False, "Disabling the gradient must release tracking of its shader.");

            profile.Customization = new CharacterCustomization { HairGradientEnabled = true };
            gradients.FrameUpdate(0f);
            var replacement = layer.Shader;
            Assert.That(replacement, Is.Not.Null);
            if (displaced)
                displacement.EnsureDisplacementIsNotOnSprite((mob, sprite), key);
            sprites.RemoveLayer((mob, sprite), key, false);
            var changed = new VisualBodyLayersChangedEvent();
            CEntMan.EventBus.RaiseLocalEvent(mob, ref changed);
            Assert.That(shaders.Contains(key), Is.False, "Removing a marking must release tracking of its gradient shader.");
            profile.Customization = originalCustomization;
        });
    }
}
// Arcane-End
