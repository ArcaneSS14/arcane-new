#nullable enable
using System.Linq;
using System.Collections.Generic;
using Content.Arcane.Server.Paint;
using Content.Arcane.Shared.Paint;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Power.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Fax;
using Content.Shared.Fax.Components;
using Content.Shared.Paper;
using Content.Shared.Stunnable;
using Content.Server.Chemistry.TileReactions;
using Content.Server.Decals;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class PaintPortTest : InteractionTest
{

    [Test]
    public async Task StrokeFloodCannotBypassTheServerMessageBudget()
    {
        await SpawnTarget("PaintCanvasSmall");
        await PlaceInHands("Paintbrush");
        await Interact();
        await Pair.RunUntilSynced();
        Assert.That(TryGetBui(PaintUiKey.Key, out var bui), Is.True);
        await Client.WaitPost(() =>
        {
            for (var i = 1; i <= 40; i++)
                bui!.SendMessage(Stroke(i, i, 3, 3));
            bui!.SendMessage(Stroke(41, 41, 5, 5));
        });
        await RunTicks(15);
        await AssertPixel(3, 3, Color.Red);
        await AssertPixel(5, 5, Color.White);
        await CloseBui(PaintUiKey.Key);
    }

    [TestCase("A small painting", "A small painting")]
    public async Task StrokeReplayUndoAndFinishCrossBui(string requestedTitle, string? expectedTitle)
    {
        await Server.WaitPost(() => SEntMan.System<MetaDataSystem>().SetEntityName(SPlayer, "Painter"));
        await SpawnTarget("PaintCanvasSmall");
        await PlaceInHands("Paintbrush");
        await Interact();
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() => Assert.That(IsUiOpen(PaintUiKey.Key), Is.True));

        await SendBui(PaintUiKey.Key, Stroke(1, 1, 3, 3));
        await AssertPixel(3, 3, Color.Red);
        await SendBui(PaintUiKey.Key, Stroke(1, 1, 5, 5));
        await AssertPixel(5, 5, Color.White);
        await SendBui(PaintUiKey.Key, new PaintUndoMessage());
        await AssertPixel(3, 3, Color.White);
        await SendBui(PaintUiKey.Key, Stroke(2, 2, 7, 7));
        await SendBui(PaintUiKey.Key, new PaintFinishMessage(requestedTitle));
        await Server.WaitAssertion(() =>
        {
            var surface = SEntMan.GetComponent<PaintSurfaceComponent>(STarget!.Value);
            Assert.That(surface.Finished, Is.True);
            Assert.That(surface.Title, Is.EqualTo(expectedTitle));
            Assert.That(surface.Artist, Is.Not.Null.And.Not.Empty);
        });
        await Client.WaitAssertion(() => Assert.That(
            CEntMan.GetComponent<PaintSurfaceComponent>(ToClient(STarget!.Value)).Title, Is.EqualTo(expectedTitle)));
        await SendBui(PaintUiKey.Key, Stroke(3, 3, 9, 9));
        await AssertPixel(9, 9, Color.White);
        await CloseBui(PaintUiKey.Key);
    }

    [TestCase(true)]
    public async Task PhotocopierCompletesOrCancelsOnPowerLoss(bool cancel)
    {
        await SpawnTarget("Photocopier");
        EntityUid original = default;
        EntityUid blank = default;
        await Server.WaitAssertion(() =>
        {
            var machine = STarget!.Value;
            var coords = SEntMan.GetComponent<TransformComponent>(machine).Coordinates;
            original = SEntMan.SpawnEntity("PaintCanvasSmall", coords);
            blank = SEntMan.SpawnEntity("PaintCanvasSmall", coords);
            var surface = SEntMan.GetComponent<PaintSurfaceComponent>(original);
            var image = new PaintImage(33, 33, Color.White);
            image.Pixels[0] = image.FindOrAddColor(Color.Red, out _);
            var paint = SEntMan.System<PaintSurfaceSystem>();
            Assert.That(paint.SetImage((original, surface), image), Is.True);
            paint.SetFinished((original, surface), "Original", "Artist", "paint-finished-name");
            var slots = SEntMan.System<ItemSlotsSystem>();
            Assert.That(slots.TryInsert(machine, "canvas_a", original, null), Is.True);
            Assert.That(slots.TryInsert(machine, "canvas_b", blank, null), Is.True);
            SEntMan.System<PowerReceiverSystem>().SetNeedsPower(machine, false);
        });
        await RunTicks(30);
        await Server.WaitAssertion(() =>
        {
            var ent = (STarget!.Value, SEntMan.GetComponent<PhotocopierComponent>(STarget.Value));
            var copier = SEntMan.System<PhotocopierSystem>();
            Assert.That(copier.TryStart(ent, SPlayer), Is.True);
            Assert.That(copier.TryStart(ent, SPlayer), Is.False);
            Assert.That(ent.Item2.State, Is.EqualTo(PhotocopierState.Scanning));
            Assert.That(SEntMan.System<ItemSlotsSystem>().TryEject(ent.Item1, "canvas_a", SPlayer, out _), Is.False);
        });
        if (cancel)
        {
            await RunTicks(30);
            await Server.WaitPost(() => SEntMan.System<PowerReceiverSystem>().SetPowerDisabled(STarget!.Value, true));
        }
        await RunTicks(360);
        await Server.WaitAssertion(() =>
        {
            var machine = STarget!.Value;
            var source = SEntMan.GetComponent<PaintSurfaceComponent>(original);
            var target = SEntMan.GetComponent<PaintSurfaceComponent>(blank);
            Assert.That(SEntMan.GetComponent<PhotocopierComponent>(machine).State, Is.EqualTo(PhotocopierState.Idle));
            var slots = SEntMan.System<ItemSlotsSystem>();
            Assert.That(slots.TryGetSlot(machine, "canvas_a", out var a), Is.True);
            Assert.That(slots.TryGetSlot(machine, "canvas_b", out var b), Is.True);
            Assert.That(a!.Locked || b!.Locked, Is.False);
            Assert.That(slots.GetItemOrNull(machine, "canvas_a"), Is.EqualTo(original));
            Assert.That(slots.GetItemOrNull(machine, "canvas_b"), Is.EqualTo(blank));
            Assert.That(target.Finished, Is.EqualTo(!cancel));
            if (cancel)
            {
                Assert.That(target.Image!.Pixels.All(p => p == 0), Is.True);
                return;
            }

            Assert.That(target.Title, Is.EqualTo(source.Title));
            Assert.That(target.Artist, Does.Contain("Artist"));
            Assert.That(target.Image!.Pixels, Is.EqualTo(source.Image!.Pixels));
            Assert.That(target.Image.Pixels, Is.Not.SameAs(source.Image.Pixels));
            Assert.That(target.Image.Palette, Is.Not.SameAs(source.Image.Palette));
            source.Image.Pixels[0] = 0;
            Assert.That(target.Image.Pixels[0], Is.Not.Zero);
            Assert.That(SEntMan.System<PhotocopierSystem>().TryStart(
                (machine, SEntMan.GetComponent<PhotocopierComponent>(machine)), SPlayer), Is.False);
        });
    }

    [TestCase("delete-source")]
    public async Task PhotocopierRejectsInvalidInputsAndCancelsDeletedInputs(string scenario)
    {
        await SpawnTarget("Photocopier");
        EntityUid original = default;
        EntityUid target = default;
        var deletion = scenario.StartsWith("delete-", StringComparison.Ordinal);
        await Server.WaitAssertion(() =>
        {
            var machine = STarget!.Value;
            var coords = SEntMan.GetComponent<TransformComponent>(machine).Coordinates;
            original = SEntMan.SpawnAtPosition("PaintCanvasSmall", coords);
            target = SEntMan.SpawnAtPosition(scenario == "size" ? "PaintCanvas" : "PaintCanvasSmall", coords);
            var paint = SEntMan.System<PaintSurfaceSystem>();
            foreach (var canvas in scenario == "two-painted" ? new[] { original, target } :
                     scenario == "two-blank" ? Array.Empty<EntityUid>() : new[] { original })
            {
                var surface = SEntMan.GetComponent<PaintSurfaceComponent>(canvas);
                var image = new PaintImage(33, 33, Color.White);
                image.Pixels[0] = image.FindOrAddColor(Color.Red, out _);
                Assert.That(paint.SetImage((canvas, surface), image), Is.True);
            }

            var slots = SEntMan.System<ItemSlotsSystem>();
            Assert.That(slots.TryInsert(machine, "canvas_a", original, null), Is.True);
            Assert.That(slots.TryInsert(machine, "canvas_b", target, null), Is.True);
            SEntMan.System<PowerReceiverSystem>().SetNeedsPower(machine, false);
        });
        await RunTicks(30);
        await Server.WaitAssertion(() =>
        {
            var machine = STarget!.Value;
            Assert.That(SEntMan.System<PhotocopierSystem>().TryStart(
                (machine, SEntMan.GetComponent<PhotocopierComponent>(machine)), SPlayer), Is.EqualTo(deletion));
            if (deletion)
                SEntMan.QueueDeleteEntity(scenario == "delete-source" ? original : target);
        });
        await RunTicks(360);
        await Server.WaitAssertion(() =>
        {
            var machine = STarget!.Value;
            Assert.That(SEntMan.GetComponent<PhotocopierComponent>(machine).State, Is.EqualTo(PhotocopierState.Idle));
            var slots = SEntMan.System<ItemSlotsSystem>();
            Assert.That(slots.TryGetSlot(machine, "canvas_a", out var a), Is.True);
            Assert.That(slots.TryGetSlot(machine, "canvas_b", out var b), Is.True);
            Assert.That(a!.Locked || b!.Locked, Is.False);
            if (SEntMan.TryGetComponent<PaintSurfaceComponent>(target, out var surface))
            {
                Assert.That(surface.Finished, Is.False);
                Assert.That(surface.Image!.Pixels.Any(p => p != 0), Is.EqualTo(scenario == "two-painted"));
                Assert.That(slots.GetItemOrNull(machine, "canvas_b"), Is.EqualTo(target));
            }
        });
    }

    private static PaintStrokeMessage Stroke(int sequence, int stroke, int x, int y) =>
        new(sequence, Color.Red, 1, PaintBrushStyle.Round, PaintShape.Freehand, false, stroke,
            new[] { new Vector2i(x, y) });

    private async Task AssertPixel(int x, int y, Color color)
    {
        await Server.WaitAssertion(() =>
        {
            var image = SEntMan.GetComponent<PaintSurfaceComponent>(STarget!.Value).Image!;
            Assert.That(image.GetPixel(y * image.Width + x), Is.EqualTo(color));
        });
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            var image = CEntMan.GetComponent<PaintSurfaceComponent>(CTarget!.Value).Image!;
            Assert.That(image.GetPixel(y * image.Width + x), Is.EqualTo(color));
        });
    }
}
