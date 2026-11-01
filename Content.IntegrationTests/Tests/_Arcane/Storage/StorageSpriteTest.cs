using System.Linq;
using Content.Client.Storage.Visualizers;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Storage.EntitySystems;
using Robust.Client.GameObjects;

namespace Content.IntegrationTests.Tests._Arcane.Storage;

public sealed class StorageSpriteTest : InteractionTest
{
    [Test]
    public async Task StorageDoorsRemainVisibleThroughRepeatedOpenClose()
    {
        var cases = new[]
        {
            ("WardrobeChapel", "chapel_door", "chapel_open"),
            ("LockerRoboticist", "robo_door", "robo_open"),
            ("WardrobeRobotics", "robo_wardrobe_door", "generic_open"),
            ("CrateLockBoxEngineering", "closed", "open"),
            ("CrateLockBoxMedical", "closed", "open"),
            ("CrateLockBoxScience", "closed", "open"),
            ("CrateLockBoxSecurity", "closed", "open"),
            ("CrateLockBoxService", "closed", "open"),
            ("CrateSyndicateSecure", "closed", "open"),
        };

        foreach (var (prototype, closed, open) in cases)
        {
            await SpawnTarget(prototype);
            await AssertDoor(prototype, closed);
            for (var i = 0; i < 2; i++)
            {
                await Server.WaitPost(() => SEntMan.System<EntityStorageSystem>().OpenStorage(STarget!.Value));
                await AssertDoor(prototype, open);
                await Server.WaitPost(() => SEntMan.System<EntityStorageSystem>().CloseStorage(STarget!.Value));
                await AssertDoor(prototype, closed);
            }
            await Delete(Target!.Value);
        }
    }

    private async Task AssertDoor(string prototype, string expected)
    {
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            var uid = CTarget!.Value;
            var sprite = CEntMan.GetComponent<SpriteComponent>(uid);
            var sprites = CEntMan.System<SpriteSystem>();
            // Crates do not map their static body; closets map a body that the visualizer updates.
            var baseIndex = sprites.LayerMapTryGet((uid, sprite), StorageVisualLayers.Base, out var mappedBase, false)
                ? mappedBase
                : sprite.AllLayers.ToList().FindIndex(layer => layer.RsiState.ToString() == "base");
            var doorIndex = sprites.LayerMapGet(uid, StorageVisualLayers.Door);
            var door = sprite[doorIndex];
            TestContext.Out.WriteLine($"{prototype}: base={baseIndex}, door={doorIndex}, state={door.RsiState}, visible={door.Visible}, rsi={door.ActualRsi?.Path}");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(baseIndex, Is.GreaterThanOrEqualTo(0), $"{prototype}: missing body layer");
                Assert.That(baseIndex, Is.LessThan(doorIndex), $"{prototype}: opaque body covers the door");
                Assert.That(door.Visible, Is.True, $"{prototype}: door disappeared");
                Assert.That(door.RsiState.ToString(), Is.EqualTo(expected), $"{prototype}: wrong door state");
                Assert.That(door.ActualRsi, Is.Not.Null, $"{prototype}: door has no RSI");
                Assert.That(door.ActualRsi?.TryGetState(door.RsiState, out _), Is.True,
                    $"{prototype}: selected door state cannot render");
                Assert.That(door.ActualRsi?.Size.Y, Is.EqualTo(48), $"{prototype}: old short crate/closet sprite");
                if (prototype.StartsWith("CrateLockBox"))
                {
                    var openIndex = sprites.LayerMapGet(uid, "open_overlay");
                    var closedIndex = sprites.LayerMapGet(uid, "lid_overlay");
                    Assert.That(openIndex, Is.GreaterThan(doorIndex), $"{prototype}: open tint covered by lid");
                    Assert.That(closedIndex, Is.GreaterThan(doorIndex), $"{prototype}: closed tint covered by lid");
                    Assert.That(sprite[openIndex].Visible, Is.EqualTo(expected == "open"));
                    Assert.That(sprite[closedIndex].Visible, Is.EqualTo(expected == "closed"));
                }
            }
        });
    }
}
