#nullable enable
using System.Linq;
using System.Numerics;
using Content.Arcane.Shared.Clothing;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Clothing.Components;
using Content.Shared.Foldable;
using Content.Shared.Inventory;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Content.Shared.Verbs; // Arcane

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class FoldedUniformTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    // The approved complete-variant subset, independently checked against source RSI metadata.
    private static readonly string[] Supported =
    [
        "ClothingUniformJumpskirtAtmos",
        "ClothingUniformJumpskirtChemistry",
        "ClothingUniformJumpskirtChiefEngineer",
        "ClothingUniformJumpskirtCMO",
        "ClothingUniformJumpskirtDetective",
        "ClothingUniformJumpskirtDetectiveGrey",
        "ClothingUniformJumpskirtEngineering",
        "ClothingUniformJumpskirtGenetics",
        "ClothingUniformJumpskirtHoP",
        "ClothingUniformJumpskirtHoS",
        "ClothingUniformJumpskirtHoSAlt",
        "ClothingUniformJumpskirtHydroponics",
        "ClothingUniformJumpskirtJanitor",
        "ClothingUniformJumpskirtMedicalDoctor",
        "ClothingUniformJumpskirtOperative",
        "ClothingUniformJumpskirtSec",
        "ClothingUniformJumpskirtSecGrey",
        "ClothingUniformJumpskirtSeniorEngineer",
        "ClothingUniformJumpskirtSeniorOfficer",
        "ClothingUniformJumpskirtSeniorPhysician",
        "ClothingUniformJumpskirtSeniorResearcher",
        "ClothingUniformJumpskirtVirology",
        "ClothingUniformJumpskirtWarden",
        "ClothingUniformJumpsuitAtmos",
        "ClothingUniformJumpsuitCaptain",
        "ClothingUniformJumpsuitChemistry",
        "ClothingUniformJumpsuitChiefEngineer",
        "ClothingUniformJumpsuitCMO",
        "ClothingUniformJumpsuitDetective",
        "ClothingUniformJumpsuitDetectiveGrey",
        "ClothingUniformJumpsuitEngineering",
        "ClothingUniformJumpsuitEngineeringHazard",
        "ClothingUniformJumpsuitHoP",
        "ClothingUniformJumpsuitHoS",
        "ClothingUniformJumpsuitHoSBlue",
        "ClothingUniformJumpsuitHoSGrey",
        "ClothingUniformJumpsuitHydroponics",
        "ClothingUniformJumpsuitMedicalDoctor",
        "ClothingUniformJumpsuitOperative",
        "ClothingUniformJumpsuitSecGrey",
        "ClothingUniformJumpsuitSeniorEngineer",
        "ClothingUniformJumpsuitSeniorPhysician",
        "ClothingUniformJumpsuitSeniorResearcher",
        "ClothingUniformJumpsuitVirology",
        "ClothingUniformJumpsuitAtmosSyndie",
        "ClothingUniformJumpsuitAtmosSyndieAdvanced",
    ];

    // Includes all 65 omitted candidates and the 17 unsupported inherited variants.
    private static readonly string[] Unsupported =
    [
        "ClothingUniformJumpskirtCaptain",
        "ClothingUniformJumpskirtCargo",
        "ClothingUniformJumpskirtColorBlack",
        "ClothingUniformJumpskirtColorBlue",
        "ClothingUniformJumpskirtColorBrown",
        "ClothingUniformJumpskirtColorDarkBlue",
        "ClothingUniformJumpskirtColorDarkGreen",
        "ClothingUniformJumpskirtColorGreen",
        "ClothingUniformJumpskirtColorGrey",
        "ClothingUniformJumpskirtColorLightBrown",
        "ClothingUniformJumpskirtColorMaroon",
        "ClothingUniformJumpskirtColorOrange",
        "ClothingUniformJumpskirtColorPink",
        "ClothingUniformJumpskirtColorPurple",
        "ClothingUniformJumpskirtColorRed",
        "ClothingUniformJumpskirtColorTeal",
        "ClothingUniformJumpskirtColorWhite",
        "ClothingUniformJumpskirtColorYellow",
        "ClothingUniformJumpskirtMime",
        "ClothingUniformJumpskirtParamedic",
        "ClothingUniformJumpskirtPrisoner",
        "ClothingUniformJumpskirtQM",
        "ClothingUniformJumpskirtQMTurtleneck",
        "ClothingUniformJumpskirtRoboticist",
        "ClothingUniformJumpskirtScientist",
        "ClothingUniformJumpsuitAncient",
        "ClothingUniformJumpsuitAncientUnremoveable",
        "ClothingUniformJumpsuitBartender",
        "ClothingUniformJumpsuitCargo",
        "ClothingUniformJumpsuitChef",
        "ClothingUniformJumpsuitColorBlack",
        "ClothingUniformJumpsuitColorBlue",
        "ClothingUniformJumpsuitColorBrown",
        "ClothingUniformJumpsuitColorDarkBlue",
        "ClothingUniformJumpsuitColorDarkGreen",
        "ClothingUniformJumpsuitColorGreen",
        "ClothingUniformJumpsuitColorGrey",
        "ClothingUniformJumpsuitColorLightBrown",
        "ClothingUniformJumpsuitColorMaroon",
        "ClothingUniformJumpsuitColorOrange",
        "ClothingUniformJumpsuitColorPink",
        "ClothingUniformJumpsuitColorPurple",
        "ClothingUniformJumpsuitColorRed",
        "ClothingUniformJumpsuitColorTeal",
        "ClothingUniformJumpsuitColorWhite",
        "ClothingUniformJumpsuitColorYellow",
        "ClothingUniformJumpsuitGenetics",
        "ClothingUniformJumpsuitHoSAlt",
        "ClothingUniformJumpsuitJanitor",
        "ClothingUniformJumpsuitMime",
        "ClothingUniformJumpsuitParamedic",
        "ClothingUniformJumpsuitPrisoner",
        "ClothingUniformJumpsuitPsychologist",
        "ClothingUniformJumpsuitQM",
        "ClothingUniformJumpsuitQMTurtleneck",
        "ClothingUniformJumpsuitRoboticist",
        "ClothingUniformJumpsuitSalvageSpecialist",
        "ClothingUniformJumpsuitScientist",
        "ClothingUniformJumpsuitSec",
        "ClothingUniformJumpsuitSeniorOfficer",
        "ClothingUniformJumpsuitShaftMiner",
        "ClothingUniformJumpsuitWarden",
        "UniformScrubsColorBlue",
        "UniformScrubsColorGreen",
        "UniformScrubsColorPurple",
        "ClothingUniformJumpskirtWhiteCaptain",
        "ClothingUniformJumpsuitMilitaryAtmospherics",
        "ClothingUniformJumpsuitMilitaryCaptain",
        "ClothingUniformJumpsuitMilitaryCargo",
        "ClothingUniformJumpsuitMilitaryChemistry",
        "ClothingUniformJumpsuitMilitaryCMO",
        "ClothingUniformJumpsuitMilitaryEngineering",
        "ClothingUniformJumpsuitMilitaryEngineeringChief",
        "ClothingUniformJumpsuitMilitaryGenetics",
        "ClothingUniformJumpsuitMilitaryHoP",
        "ClothingUniformJumpsuitMilitaryHydroponics",
        "ClothingUniformJumpsuitMilitaryMedicalDoctor",
        "ClothingUniformJumpsuitMilitaryParamedic",
        "ClothingUniformJumpsuitMilitaryQM",
        "ClothingUniformJumpsuitMilitarySalvageSpecialist",
        "ClothingUniformJumpsuitMilitaryScientist",
        "ClothingUniformJumpsuitMilitaryVirology",
    ];

    [Test]
    public async Task SupportedUniformsFoldWhileWornAndRestoreTheirOriginalVisuals()
    {
        foreach (var prototype in Supported)
        {
            var item = await Spawn(prototype);
            string? originalClothingRsi = null;
            await Server.WaitAssertion(() =>
            {
                var uid = ToServer(item);
                originalClothingRsi = SEntMan.GetComponent<ClothingComponent>(uid).RsiPath;
                Assert.That(SEntMan.System<InventorySystem>().TryEquip(SPlayer, uid, "jumpsuit", force: true), Is.True, prototype);
                Assert.That(SEntMan.GetComponent<FoldableClothingComponent>(uid).FoldedEquippedPrefix, Is.Null);
            });
            await Pair.RunUntilSynced();

            (string Rsi, string State, Color Color, Vector2 Scale, int Count) normal = (string.Empty, string.Empty, Color.White, Vector2.One, 0);
            await Client.WaitAssertion(() => normal = WornVisual(CPlayer));
            foreach (var folded in new[] {true, false, true, false})
            {
                await Server.WaitAssertion(() =>
                {
                    var uid = ToServer(item);
                    var fold = SEntMan.GetComponent<FoldableComponent>(uid);
                    Assert.That(SEntMan.System<FoldableSystem>().TrySetFolded(uid, fold, folded, SPlayer), Is.True, prototype);
                    var clothing = SEntMan.GetComponent<ClothingComponent>(uid);
                    Assert.That(clothing.RsiPath, Is.EqualTo(originalClothingRsi));
                    Assert.That(clothing.EquippedPrefix, Is.Null);
                });
                await Pair.RunUntilSynced();
                await Client.WaitAssertion(() =>
                {
                    var uid = ToClient(item);
                    Assert.That(CEntMan.GetComponent<FoldableComponent>(uid).IsFolded, Is.EqualTo(folded), prototype);
                    var current = WornVisual(CPlayer);
                    var foldedRsi = CEntMan.GetComponent<FoldedClothingVisualsComponent>(uid).RsiPath;
                    Assert.That(current.Rsi, Is.EqualTo(folded ? $"/Textures/{foldedRsi}" : normal.Rsi), prototype);
                    Assert.That(current.State, Is.EqualTo(folded ? $"folded-{normal.State}" : normal.State), prototype);
                    Assert.That(current.Color, Is.EqualTo(normal.Color));
                    Assert.That(current.Scale, Is.EqualTo(normal.Scale));
                    Assert.That(current.Count, Is.EqualTo(normal.Count), "Repeated folding must not accumulate wearer layers.");
                });
            }

            await Server.WaitAssertion(() =>
                Assert.That(SEntMan.System<InventorySystem>().TryUnequip(SPlayer, "jumpsuit", force: true), Is.True));
            await Pair.RunUntilSynced();
            await Client.WaitAssertion(() =>
                Assert.That(CEntMan.System<SpriteSystem>().LayerMapTryGet(CPlayer, "jumpsuit-0", out _, false), Is.False));
        }
    }

    [Test]
    public async Task UnsupportedVariantsCannotFoldAfterServerSpawnAndReplication()
    {
        foreach (var prototype in Unsupported)
        {
            var item = await Spawn(prototype);
            await Server.WaitAssertion(() =>
            {
                var uid = ToServer(item);
                AssertCannotFold(SEntMan, uid, SPlayer, prototype);
            });
            await Pair.RunUntilSynced();
            await Client.WaitAssertion(() =>
            {
                var uid = ToClient(item);
                AssertCannotFold(CEntMan, uid, CPlayer, prototype);
            });
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task FoldBeforeEquipUsesMonkeyFramesAndComponentRemovalRestoresTheNormalLayer(bool removeFoldable)
    {
        var wearer = await Spawn("MobMonkey");
        var item = await Spawn("ClothingUniformJumpsuitHydroponics");
        await Server.WaitAssertion(() =>
        {
            var uid = ToServer(item);
            SEntMan.System<FoldableSystem>().SetFolded(uid, SEntMan.GetComponent<FoldableComponent>(uid), true);
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(SPlayer, ToServer(wearer), uid, "jumpsuit", force: true), Is.True);
        });
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            var visual = WornVisual(ToClient(wearer));
            Assert.That(visual.State, Is.EqualTo("folded-equipped-INNERCLOTHING-monkey"));
            Assert.That(visual.Rsi, Does.StartWith("/Textures/_Arcane/Clothing/Uniforms/Folded/"));
        });

        await Server.WaitPost(() =>
        {
            if (removeFoldable)
                SEntMan.RemoveComponent<FoldableComponent>(ToServer(item));
            else
                SEntMan.RemoveComponent<FoldedClothingVisualsComponent>(ToServer(item));
        });
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            var visual = WornVisual(ToClient(wearer));
            Assert.That(visual.State, Is.EqualTo("equipped-INNERCLOTHING-monkey"));
            Assert.That(visual.Rsi, Is.EqualTo("/Textures/Clothing/Uniforms/Jumpsuit/hydro.rsi"));
        });
    }

    [Test]
    public async Task UnsupportedMilitaryUniformsHaveNoFoldBeforeMapInitialization()
    {
        foreach (var prototype in new[]
        {
            "ClothingUniformJumpsuitMilitaryAtmospherics",
            "ClothingUniformJumpsuitMilitaryCaptain",
            "ClothingUniformJumpsuitMilitaryChemistry",
            "ClothingUniformJumpsuitMilitaryCMO",
            "ClothingUniformJumpsuitMilitaryEngineering",
            "ClothingUniformJumpsuitMilitaryEngineeringChief",
            "ClothingUniformJumpsuitMilitaryHoP",
            "ClothingUniformJumpsuitMilitaryHydroponics",
            "ClothingUniformJumpsuitMilitaryMedicalDoctor",
            "ClothingUniformJumpsuitMilitaryVirology",
        })
        {
            await Server.WaitAssertion(() => AssertDisabledInNullspace(SEntMan, SPlayer, prototype));
            await Client.WaitAssertion(() => AssertDisabledInNullspace(CEntMan, CPlayer, prototype));
        }
    }

    private static void AssertDisabledInNullspace(IEntityManager entities, EntityUid user, string prototype)
    {
        var uid = entities.Spawn(prototype, doMapInit: false);
        try
        {
            Assert.That(entities.GetComponent<MetaDataComponent>(uid).EntityLifeStage,
                Is.LessThan(EntityLifeStage.MapInitialized), prototype);
            AssertCannotFold(entities, uid, user, prototype);
        }
        finally
        {
            entities.DeleteEntity(uid);
        }
    }

    private static void AssertCannotFold(IEntityManager entities, EntityUid uid, EntityUid user, string prototype)
    {
        if (!entities.TryGetComponent<FoldableComponent>(uid, out var foldable))
        {
            Assert.That(entities.HasComponent<FoldableClothingComponent>(uid), Is.False, prototype);
            Assert.That(entities.HasComponent<FoldedClothingVisualsComponent>(uid), Is.False, prototype);
            return;
        }

        Assert.That(foldable.Enabled, Is.False, prototype);
        Assert.That(entities.GetComponent<FoldedClothingVisualsComponent>(uid).Enabled, Is.False, prototype);
        var clothing = entities.GetComponent<ClothingComponent>(uid);
        var original = (clothing.RsiPath, clothing.EquippedPrefix);
        var folding = entities.System<FoldableSystem>();
        Assert.That(folding.CanToggleFold(uid, foldable), Is.False, prototype);
        Assert.That(folding.TrySetFolded(uid, foldable, true, user), Is.False, prototype);
        folding.SetFolded(uid, foldable, true, user);
        Assert.That(foldable.IsFolded, Is.False, prototype);
        Assert.That((clothing.RsiPath, clothing.EquippedPrefix), Is.EqualTo(original), prototype);
        var verbs = entities.System<SharedVerbSystem>().GetLocalVerbs(uid, user, typeof(AlternativeVerb), force: true);
        Assert.That(verbs.Select(verb => verb.Text), Does.Not.Contain(Loc.GetString(foldable.FoldVerbText)), prototype);
        Assert.That(entities.HasComponent<HideLayerClothingComponent>(uid), Is.False, prototype);
    }

    private (string Rsi, string State, Color Color, Vector2 Scale, int Count) WornVisual(EntityUid wearer)
    {
        var sprite = CEntMan.GetComponent<SpriteComponent>(wearer);
        Assert.That(CEntMan.System<SpriteSystem>().LayerMapTryGet((wearer, sprite), "jumpsuit-0", out var index, false), Is.True);
        var layer = sprite[index];
        Assert.That(layer.ActualRsi, Is.Not.Null);
        var state = layer.RsiState.ToString();
        Assert.That(state, Is.Not.Null);
        return (layer.ActualRsi!.Path.ToString(), state!, layer.Color, layer.Scale, sprite.AllLayers.Count());
    }
}
