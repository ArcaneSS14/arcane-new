using Content.Arcane.Client.Storage;
using Content.Client.Storage.Visualizers;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Storage.EntitySystems;
using Content.Shared.SprayPainter.Prototypes;
using Robust.Client.GameObjects;
using System.Numerics;
using Robust.Shared.Timing;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.IntegrationTests.Tests._Arcane.Storage;

public sealed class StorageAnimationTest : InteractionTest
{
    [TestPrototypes]
    private const string Prototypes = """
- type: entity
  parent: ClosetMaintenance
  id: TestAnimatedCloset
  components:
  - type: StorageAnimationVisuals
    duration: 1

- type: entity
  parent: LockerSteel
  id: TestAnimatedLocker
  components:
  - type: StorageAnimationVisuals
    duration: 1

- type: entity
  parent: WardrobeChapelFilled
  id: TestAnimatedChapel
  components:
  - type: StorageAnimationVisuals
    duration: 1

- type: entity
  parent: ClosetWall
  id: TestAnimatedWallCloset
  components:
  - type: StorageAnimationVisuals
    duration: 1

- type: entity
  parent: CrateGenericSteel
  id: TestAnimatedCrate
  components:
  - type: StorageAnimationVisuals
    duration: 1

- type: entity
  parent: CratePirateChest
  id: TestAnimatedPirate
  components:
  - type: StorageAnimationVisuals
    duration: 1

- type: entity
  parent: CrateToyBox
  id: TestAnimatedToy
  components:
  - type: StorageAnimationVisuals
    duration: 1

- type: entity
  parent: CrateFunBingleLoot
  id: TestAnimatedBingle
  components:
  - type: StorageAnimationVisuals
    duration: 1

- type: entity
  parent: CrateWeb
  id: TestAnimatedWeb
  components:
  - type: StorageAnimationVisuals
    duration: 1

- type: entity
  parent: CrateTrashCart
  id: TestAnimatedTrash
  components:
  - type: StorageAnimationVisuals
    duration: 1

- type: entity
  parent: CrateLockBoxEngineering
  id: TestAnimatedLockBox
  components:
  - type: StorageAnimationVisuals
    duration: 1

- type: entity
  parent: LavalandCrateNecropolis
  id: TestAnimatedNecropolis
  components:
  - type: StorageAnimationVisuals
    duration: 1
""";

    [Test]
    public async Task ReversalCompletionAndDeletionDoNotLeaveDetachedDoors()
    {
        foreach (var prototype in new[] { "TestAnimatedCloset", "TestAnimatedLocker", "TestAnimatedChapel", "TestAnimatedWallCloset", "TestAnimatedCrate",
                     "TestAnimatedPirate", "TestAnimatedToy", "TestAnimatedBingle", "TestAnimatedWeb", "TestAnimatedTrash", "TestAnimatedLockBox" })
        {
            await SpawnTarget(prototype);
            await Pair.RunUntilSynced();
            await Client.WaitAssertion(() =>
            {
                var comp = CEntMan.GetComponent<StorageAnimationVisualsComponent>(CTarget!.Value);
                Assert.That(comp.AppearanceInitialized, Is.True, prototype);
                Assert.That(comp.Door, Is.Null, "Initial appearance must not animate.");
            });

            await Server.WaitPost(() => SEntMan.System<EntityStorageSystem>().OpenStorage(STarget!.Value));
            await RunSeconds(0.4f);
            EntityUid door = default;
            await Client.WaitAssertion(() =>
            {
                var uid = CTarget!.Value;
                var comp = CEntMan.GetComponent<StorageAnimationVisualsComponent>(uid);
                Assert.That(comp.Active, Is.True, prototype);
                Assert.That(comp.Door, Is.Not.Null);
                door = comp.Door!.Value;
                Assert.That(CEntMan.GetComponent<TransformComponent>(door).ParentUid, Is.EqualTo(uid));
                var sprite = CEntMan.GetComponent<SpriteComponent>(door);
                Assert.That(sprite.Visible, Is.True);
                Assert.That(CEntMan.System<SpriteSystem>().TryGetLayer((door, sprite), comp.DoorLayer, out var doorLayer, false), Is.True);
                var transform = doorLayer!.LocalMatrix * sprite.LocalMatrix;
                Assert.That(comp.Lid ? transform.M22 : transform.M11, Is.LessThan(0.95f), "The door must actually move.");
                var owner = CEntMan.GetComponent<SpriteComponent>(uid);
                var hinge = Vector2.Transform(comp.ResolvedHinge, transform);
                Assert.That(Vector2.Distance(hinge, comp.ResolvedHinge + owner.Offset), Is.LessThan(0.001f), "The hinge must stay fixed.");
                Assert.That(sprite[comp.DoorLayer].RsiState.ToString(), Is.EqualTo(comp.ClosedState));
                if (!comp.Lid)
                    Assert.That(sprite.DrawDepth, Is.EqualTo((int) DrawDepth.OverMobs));
                else
                    Assert.That(sprite.DrawDepth, Is.GreaterThan(owner.DrawDepth), "The lid must stay above the crate body.");

                var phase = comp.Phase;
                var tick = CGameTiming.CurTick;
                try
                {
                    CGameTiming.CurTick = new GameTick(tick.Value - 10);
                    CEntMan.System<StorageAnimationSystem>().FrameUpdate(0.01f);
                    Assert.That(comp.Phase, Is.GreaterThan(phase), "A clock correction must not rewind the animation.");
                }
                finally
                {
                    CGameTiming.CurTick = tick;
                }
            });

            await RunSeconds(0.4f);
            await Client.WaitAssertion(() =>
            {
                var comp = CEntMan.GetComponent<StorageAnimationVisualsComponent>(CTarget!.Value);
                var sprite = CEntMan.GetComponent<SpriteComponent>(door);
                Assert.That(sprite[comp.DoorLayer].RsiState.ToString(), Is.EqualTo(comp.OpenState));
                if (!comp.Lid)
                    Assert.That(sprite.DrawDepth, Is.EqualTo((int) DrawDepth.BelowMobs));
            });

            await Server.WaitPost(() => SEntMan.System<EntityStorageSystem>().CloseStorage(STarget!.Value));
            await RunSeconds(0.05f);
            await Pair.RunUntilSynced();
            await Client.WaitAssertion(() =>
            {
                var comp = CEntMan.GetComponent<StorageAnimationVisualsComponent>(CTarget!.Value);
                Assert.That(comp.Open, Is.False, prototype);
                Assert.That(comp.Door, Is.EqualTo(door), "Reversal must reuse the moving door.");
            });

            await RunSeconds(1.1f);
            await Client.WaitAssertion(() =>
            {
                var uid = CTarget!.Value;
                var comp = CEntMan.GetComponent<StorageAnimationVisualsComponent>(uid);
                var sprite = CEntMan.GetComponent<SpriteComponent>(uid);
                var index = CEntMan.System<SpriteSystem>().LayerMapGet(uid, StorageVisualLayers.Door);
                Assert.That(comp.Door, Is.Null);
                Assert.That(CEntMan.Deleted(door), Is.True);
                Assert.That(sprite[index].Visible, Is.True, "Completion must restore the ordinary door.");
            });

            await Server.WaitPost(() => SEntMan.System<EntityStorageSystem>().OpenStorage(STarget!.Value));
            await RunSeconds(0.1f);
            await Client.WaitAssertion(() =>
            {
                var comp = CEntMan.GetComponent<StorageAnimationVisualsComponent>(CTarget!.Value);
                Assert.That(comp.Door, Is.Not.Null);
                door = comp.Door!.Value;
            });
            await Delete(Target!.Value);
            await RunSeconds(0.1f);
            await Client.WaitAssertion(() => Assert.That(CEntMan.Deleted(door), Is.True));
        }
    }

    [Test]
    public async Task SupportedArtAndExceptionsAreSelectedByTheirActualAppearance()
    {
        var supported = new[]
        {
            "ClosetTool", "ClosetEmergency", "LockerCaptain",
            "WardrobeRobotics", "WardrobeChapelFilled", "LockerNanorep",
            "ClosetWallEmergency", "LockerWallMedical", "LockerWallSyndicate",
            "CrateFreezer", "CrateEngineeringSecure", "CrateAtmosphericsSecure",
            "CrateWooden", "CrateWeaponSecure", "CrateRobotics",
            "CrateRoboticsSecure", "CrateAtmospherics", "CrateCargo",
            "CrateDeForest", "CrateFreezerDonk", "CrateGolden",
            "CrateFreezerKitchen", "CrateLavender", "CrateMedicalAlt",
            "CrateMining", "CrateNakamura", "CratePink",
            "CrateRobust", "CrateSELF", "CrateSilver",
            "CrateAnimalRightsConsortiumSecure", "CrateBitrunSecure", "CrateCargoSecure",
            "CrateCybersunDawnSecure", "CrateCybersunDuskSecure", "CrateCybersunNightSecure",
            "CrateCybersunNoonSecure", "CrateFreezerDonkSecure", "CrateGorlexSecure",
            "CrateGorlexWeaponSecure", "CrateInterdyneSecure", "CrateFreezerInterdyneSecure",
            "CrateFreezerKitchenSecure", "CrateMI13Secure", "CrateMiningSecure",
            "CrateRadiationSecure", "CrateSELFSecure", "CrateTigerSecure",
            "CrateWaffleSecure", "CrateInteQ", "CrateInteQSecure",
            "CrateGenericSteel", "CrateHydroponics", "CrateMedical",
            "CrateRadiation", "CrateInternals", "CrateElectrical",
            "CrateEngineering", "CrateScience", "CrateSurgery",
            "CrateSecgear", "CrateMedicalSecure", "CrateChemistrySecure",
            "CratePrivateSecure", "CrateScienceSecure", "CratePlasma",
            "CrateSecure", "CrateHydroSecure", "CrateCommandSecure",
            "CrateSyndicateSecure", "CrateSyndicate",
            "CratePirateChest", "CrateToyBox", "CrateFunBingleLoot", "CrateWeb", "CrateTrashCart",
            "LavalandCrateNecropolis", "CrateLockBoxEngineering", "CrateLockBoxMedical", "CrateLockBoxScience", "CrateLockBoxSecurity", "CrateLockBoxService",
        };
        var exceptions = new[]
        {
            "CratePlastic", "CrateLivestock", "CrateRodentCage", "CrateCoffin",
            "LockerBooze",
        };
        foreach (var prototypes in new[] { supported, exceptions })
        foreach (var prototype in prototypes)
        {
            await SpawnTarget(prototype);
            await Pair.RunUntilSynced();
            await Client.WaitAssertion(() =>
            {
                var animated = CEntMan.TryGetComponent<StorageAnimationVisualsComponent>(CTarget!.Value, out var comp)
                               && comp.AppearanceInitialized;
                var sprite = CEntMan.GetComponent<SpriteComponent>(CTarget!.Value);
                var visuals = CEntMan.GetComponent<EntityStorageVisualsComponent>(CTarget!.Value);
                Assert.That(animated, Is.EqualTo(prototypes == supported),
                    $"{prototype}: {sprite.BaseRSI?.Path}, {visuals.StateDoorOpen}");
            });
            await Delete(Target!.Value);
        }
    }

    [Test]
    public async Task CompositeArtKeepsTheBodyStationaryAndCleansUpBothParts()
    {
        foreach (var prototype in new[] { "TestAnimatedLockBox", "TestAnimatedNecropolis" })
        {
            await SpawnTarget(prototype);
            await Pair.RunUntilSynced();
            await Server.WaitPost(() => SEntMan.System<EntityStorageSystem>().OpenStorage(STarget!.Value));
            await RunSeconds(0.4f);
            EntityUid fixedPart = default;
            await Client.WaitAssertion(() =>
            {
                var uid = CTarget!.Value;
                var comp = CEntMan.GetComponent<StorageAnimationVisualsComponent>(uid);
                Assert.That(comp.FixedOverlay, Is.Not.Null, prototype);
                fixedPart = comp.FixedOverlay!.Value;
                var body = CEntMan.GetComponent<SpriteComponent>(fixedPart);
                var owner = CEntMan.GetComponent<SpriteComponent>(uid);
                Assert.That(body.LocalMatrix, Is.EqualTo(owner.LocalMatrix), "The body must not move with the lid.");
                var moving = CEntMan.GetComponent<SpriteComponent>(comp.Door!.Value);
                if (comp.BodyCutoff > 0)
                {
                    Assert.That(owner[comp.HiddenBaseLayer!.Value].Visible, Is.False);
                    Assert.That(moving[comp.DoorLayer].Texture!.Height, Is.EqualTo(comp.BodyCutoff));
                }
                else
                {
                    var sprites = CEntMan.System<SpriteSystem>();
                    var front = sprites.LayerMapGet(uid, "lid_overlay");
                    var back = sprites.LayerMapGet(uid, "open_overlay");
                    Assert.That(owner[front].Visible || owner[back].Visible, Is.False);
                    Assert.That(moving[front].Visible, Is.True);
                    Assert.That(moving[back].Visible, Is.False);
                    Assert.That(body[front].Texture!.Height, Is.EqualTo(48 - comp.OverlayCutoff));
                    Assert.That(moving[front].Color, Is.EqualTo(owner[front].Color));
                }
            });
            await RunSeconds(0.4f);
            await Client.WaitAssertion(() =>
            {
                var uid = CTarget!.Value;
                var comp = CEntMan.GetComponent<StorageAnimationVisualsComponent>(uid);
                var moving = CEntMan.GetComponent<SpriteComponent>(comp.Door!.Value);
                if (comp.BodyCutoff > 0)
                    Assert.That(moving[comp.DoorLayer].Texture, Is.SameAs(comp.BackTexture));
                else
                {
                    var sprites = CEntMan.System<SpriteSystem>();
                    Assert.That(moving[sprites.LayerMapGet(uid, "lid_overlay")].Visible, Is.False);
                    Assert.That(moving[sprites.LayerMapGet(uid, "open_overlay")].Visible, Is.True);
                }
            });
            await RunSeconds(0.4f);
            await Client.WaitAssertion(() => Assert.That(CEntMan.Deleted(fixedPart), Is.True));
            await Delete(Target!.Value);
        }
    }

    [Test]
    public async Task RemovingAnimationRestoresTheStaticDoor()
    {
        await SpawnTarget("TestAnimatedCloset");
        await Pair.RunUntilSynced();
        await Server.WaitPost(() => SEntMan.System<EntityStorageSystem>().OpenStorage(STarget!.Value));
        await RunSeconds(0.4f);
        EntityUid door = default;
        await Client.WaitPost(() =>
        {
            var uid = CTarget!.Value;
            door = CEntMan.GetComponent<StorageAnimationVisualsComponent>(uid).Door!.Value;
            CEntMan.RemoveComponent<StorageAnimationVisualsComponent>(uid);
        });
        await RunSeconds(0.1f);
        await Client.WaitAssertion(() =>
        {
            var uid = CTarget!.Value;
            var sprite = CEntMan.GetComponent<SpriteComponent>(uid);
            var index = CEntMan.System<SpriteSystem>().LayerMapGet(uid, StorageVisualLayers.Door);
            Assert.That(CEntMan.Deleted(door), Is.True);
            Assert.That(sprite[index].Visible, Is.True);
            Assert.That(sprite[index].RsiState.ToString(), Is.EqualTo("generic_open"));
        });
    }

    [Test]
    public async Task RepaintingToUnsupportedArtCancelsTheMovingDoor()
    {
        await SpawnTarget("TestAnimatedCloset");
        await Pair.RunUntilSynced();
        await Server.WaitPost(() => SEntMan.System<EntityStorageSystem>().OpenStorage(STarget!.Value));
        await RunSeconds(0.4f);
        EntityUid door = default;
        await Client.WaitAssertion(() =>
        {
            var comp = CEntMan.GetComponent<StorageAnimationVisualsComponent>(CTarget!.Value);
            Assert.That(comp.Door, Is.Not.Null);
            door = comp.Door!.Value;
        });
        await Server.WaitPost(() => SEntMan.System<SharedAppearanceSystem>()
            .SetData(STarget!.Value, PaintableVisuals.Prototype, "LockerBooze"));
        await RunSeconds(0.1f);
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            var uid = CTarget!.Value;
            var comp = CEntMan.GetComponent<StorageAnimationVisualsComponent>(uid);
            var sprite = CEntMan.GetComponent<SpriteComponent>(uid);
            Assert.That(comp.Active, Is.False);
            Assert.That(comp.AppearanceInitialized, Is.False);
            Assert.That(CEntMan.Deleted(door), Is.True);
            Assert.That(sprite[comp.DoorLayer].Visible, Is.True);
            Assert.That(CEntMan.GetComponent<EntityStorageVisualsComponent>(uid).StateDoorOpen, Is.EqualTo("cabinet_open"));
        });
    }

    [Test]
    public async Task DetachingAndReattachingSnapsToTheCurrentAppearance()
    {
        await SpawnTarget("TestAnimatedCloset");
        await Pair.RunUntilSynced();
        await Server.WaitPost(() => SEntMan.System<EntityStorageSystem>().OpenStorage(STarget!.Value));
        await RunSeconds(0.4f);
        EntityUid door = default;
        await Client.WaitAssertion(() =>
        {
            var uid = CTarget!.Value;
            var comp = CEntMan.GetComponent<StorageAnimationVisualsComponent>(uid);
            Assert.That(comp.Door, Is.Not.Null);
            door = comp.Door!.Value;
            var transform = CEntMan.GetComponent<TransformComponent>(uid);
            var coordinates = transform.Coordinates;
            var transforms = CEntMan.System<SharedTransformSystem>();
            transforms.DetachEntity(uid);
            transforms.SetCoordinates(uid, coordinates);
            var sprite = CEntMan.GetComponent<SpriteComponent>(uid);
            var appearance = CEntMan.GetComponent<AppearanceComponent>(uid);
            CEntMan.System<AppearanceSystem>().OnChangeData(uid, sprite, appearance);
            Assert.That(comp.Active, Is.False);
            Assert.That(comp.Door, Is.Null);
            Assert.That(comp.Phase, Is.EqualTo(1f));
            Assert.That(sprite[comp.DoorLayer].Visible, Is.True);
        });
        await RunSeconds(0.1f);
        await Client.WaitAssertion(() => Assert.That(CEntMan.Deleted(door), Is.True));
    }
}
