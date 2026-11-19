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
// Arcane-Start
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Chemistry.EntitySystems;
// Arcane-End

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class PaintPortTest : InteractionTest
{
    private static readonly ProtoId<ReagentPrototype> PaintReagent = "Water"; // Arcane

    // Arcane-Start
    [TestCase(false)]
    [TestCase(true)]
    public async Task StrokeCannotExceedAvailablePaintOrCharges(bool solution)
    {
        await SpawnTarget("PaintCanvasSmall");
        var tool = ToServer(await PlaceInHands(solution ? "Beaker" : "Paintbrush"));
        await Server.WaitAssertion(() =>
        {
            var paint = SEntMan.System<PaintSurfaceSystem>();
            var surface = SEntMan.GetComponent<PaintSurfaceComponent>(STarget!.Value);
            paint.SetImage((STarget.Value, surface), new PaintImage(surface.Size.X, surface.Size.Y, Color.Black));
            var settings = SEntMan.EnsureComponent<PaintToolComponent>(tool);
            if (solution)
            {
                settings.ColorMode = PaintColorMode.Solution;
                settings.Solution = "beaker";
                settings.PixelsPerUnit = 128;
                var solutions = SEntMan.System<SharedSolutionContainerSystem>();
                Assert.That(solutions.TryGetSolution(tool, "beaker", out _, out var contents), Is.True);
                contents!.AddReagent(PaintReagent, FixedPoint2.FromHundredths(1));
            }
            else
            {
                settings.PixelsPerCharge = 1;
                SEntMan.EnsureComponent<LimitedChargesComponent>(tool);
                SEntMan.System<SharedChargesSystem>().SetCharges(tool, 1);
            }
        });
        await Interact();
        await Pair.RunUntilSynced();
        await SendBui(PaintUiKey.Key, new PaintStrokeMessage(1, Color.Red, 1, PaintBrushStyle.Round,
            PaintShape.Freehand, false, 1, [new Vector2i(1, 1), new Vector2i(3, 1)]));
        await Server.WaitAssertion(() =>
        {
            var image = SEntMan.GetComponent<PaintSurfaceComponent>(STarget!.Value).Image!;
            Assert.That(image.Pixels.All(pixel => pixel == 0), Is.True, "An unaffordable stroke is atomic.");
            Assert.That(image.Palette, Has.Count.EqualTo(1), "Rejected strokes must not consume palette entries.");
        });
        await SendBui(PaintUiKey.Key, Stroke(2, 2, 1, 1));
        await SendBui(PaintUiKey.Key, Stroke(3, 3, 3, 1));
        await Server.WaitAssertion(() =>
        {
            var image = SEntMan.GetComponent<PaintSurfaceComponent>(STarget!.Value).Image!;
            Assert.That(image.Pixels.Count(pixel => pixel != 0), Is.EqualTo(1));
            if (solution)
            {
                Assert.That(SEntMan.System<SharedSolutionContainerSystem>().TryGetSolution(tool, "beaker", out _, out var contents), Is.True);
                Assert.That(contents!.Volume, Is.EqualTo(FixedPoint2.Zero));
            }
            else
                Assert.That(SEntMan.System<SharedChargesSystem>().GetCurrentCharges(tool), Is.Zero);
        });
        await CloseBui(PaintUiKey.Key);
    }
    // Arcane-End

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
