using System.Linq;
using Content.Arcane.Client.Humanoid.DirectionalLayering;
using Content.Client.DisplacementMap;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Body;
using Content.Shared.CCVar;
using Content.Shared.DisplacementMap;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;
using Robust.Client.GameObjects;
using Robust.Shared.Enums;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class DirectionalLayeringTest : InteractionTest
{
    private static readonly ProtoId<DisplacementDataPrototype> TestDisplacement = "Dwarfism";
    protected override string PlayerPrototype => "MobVulpkanin";

    [Test]
    public async Task EarPreferenceReplicatesAndCopiesWithTheBodyAppearance()
    {
        var target = await Spawn("MobVulpkanin");
        await Server.WaitAssertion(() =>
        {
            var profile = HumanoidCharacterProfile.DefaultWithSpecies("Vulpkanin", Sex.Male);
            var appearance = profile.Appearance.WithEarsAboveHair(true);
            Assert.That(appearance.Clone().WithEyeColor(Color.Red).WithSkinColor(Color.Gray)
                .WithMarkings(appearance.Markings).EarsAboveHair, Is.True);
            Assert.That(HumanoidCharacterAppearance.EnsureValid(appearance, "Vulpkanin", Sex.Male).EarsAboveHair, Is.True);
            var body = SEntMan.System<SharedVisualBodySystem>();
            body.ApplyProfileTo(SPlayer, profile.WithCharacterAppearance(appearance));
            body.CopyAppearanceFrom(SPlayer, ToServer(target));
            Assert.That(SEntMan.GetComponent<VisualBodyComponent>(ToServer(target)).EarsAboveHair, Is.True);
        });
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            Assert.That(CEntMan.GetComponent<VisualBodyComponent>(CPlayer).EarsAboveHair, Is.True);
            Assert.That(CEntMan.GetComponent<VisualBodyComponent>(ToClient(target)).EarsAboveHair, Is.True);
        });
    }

    [Test]
    public async Task DummyDirectionChangesKeepEarOrderAndLayerMapsStable()
    {
        await Client.WaitAssertion(() =>
        {
            var manager = Client.ResolveDependency<MarkingManager>();
            var hair = manager.MarkingsByLayerAndGroupAndSex(HumanoidVisualLayers.Hair, "Vulpkanin", Sex.Male)
                .Values.First(marking => marking.Sprites.Count > 0);
            var profile = HumanoidCharacterProfile.DefaultWithSpecies("Vulpkanin", Sex.Male);
            var markings = profile.Appearance.Markings;
            var hairOrgan = manager.GetMarkingData("Vulpkanin").First(pair => pair.Value.Layers.Contains(HumanoidVisualLayers.Hair)).Key;
            var earOrgan = manager.GetMarkingData("Vulpkanin").First(pair => pair.Value.Layers.Contains(HumanoidVisualLayers.HeadTop)).Key;
            markings[hairOrgan][HumanoidVisualLayers.Hair] = [new Marking(hair.ID, hair.Sprites.Count)];
            markings[earOrgan][HumanoidVisualLayers.HeadTop] = [new Marking("VulpEar", new[] {Color.Gray, Color.White})];
            var dummy = CEntMan.SpawnEntity(CProtoMan.Index(profile.Species).DollPrototype, MapCoordinates.Nullspace);
            try
            {
                var body = CEntMan.System<SharedVisualBodySystem>();
                var ordering = CEntMan.System<DirectionalLayeringSystem>();
                var sprites = CEntMan.System<SpriteSystem>();
                var displacements = CEntMan.System<DisplacementMapSystem>();
                var sprite = CEntMan.GetComponent<SpriteComponent>(dummy);
                var hairKey = $"{hair.ID}-{((SpriteSpecifier.Rsi) hair.Sprites[0]).RsiState}";
                foreach (var above in new[] {false, true})
                {
                    body.ApplyProfileTo(dummy, profile.WithCharacterAppearance(profile.Appearance.WithEarsAboveHair(above)));
                    foreach (var direction in new[] {Direction.South, Direction.East, Direction.North, Direction.West, Direction.South})
                    {
                        Assert.That(sprites.LayerMapTryGet((dummy, sprite), "VulpEar-vulp", out var targetIndex, false), Is.True);
                        Assert.That(displacements.TryAddDisplacement(CProtoMan.Index(TestDisplacement).Displacement,
                            (dummy, sprite), targetIndex, "VulpEar-vulp", out var helperKey), Is.True);
                        var layerCount = sprite.AllLayers.Count();
                        ordering.ApplyDummyOrdering(dummy, direction);
                        Assert.That(sprites.LayerMapTryGet((dummy, sprite), "VulpEar-vulp", out var ears, false), Is.True);
                        Assert.That(sprites.LayerMapTryGet((dummy, sprite), hairKey, out var hairIndex, false), Is.True);
                        Assert.That(above ? ears > hairIndex : ears < hairIndex, Is.True, $"Ear preference {above}, view {direction}");
                        Assert.That(sprite.AllLayers.Count(), Is.EqualTo(layerCount), "Reordering must not duplicate layers.");
                        Assert.That(sprites.LayerMapTryGet((dummy, sprite), helperKey!, out var helperIndex, false), Is.True);
                        Assert.That(helperIndex, Is.LessThan(ears), "The displacement helper must remain before its target.");

                        var cfg = Client.ResolveDependency<IConfigurationManager>();
                        var censored = cfg.GetCVar(CCVars.AccessibilityClientCensorNudity);
                        cfg.SetCVar(CCVars.AccessibilityClientCensorNudity, !censored);
                        ordering.FrameUpdate(0f);
                        Assert.That(sprites.LayerMapTryGet((dummy, sprite), "VulpEar-vulp", out ears, false), Is.True);
                        Assert.That(sprites.LayerMapTryGet((dummy, sprite), hairKey, out hairIndex, false), Is.True);
                        Assert.That(above ? ears > hairIndex : ears < hairIndex, Is.True, "Local marking rebuild must retain ear order without a turn.");
                        cfg.SetCVar(CCVars.AccessibilityClientCensorNudity, censored);
                        ordering.FrameUpdate(0f);
                    }
                }
            }
            finally
            {
                CEntMan.DeleteEntity(dummy);
            }
        });
    }
}
