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
    private static readonly ProtoId<ReagentPrototype> Water = "Water";
    private static readonly ProtoId<CargoProductPrototype> ArtProduct = "FunArtSupplies";
    private static readonly ProtoId<CargoProductPrototype> PaintingProduct = "FunPaintingSupplies";

    [Test]
    public async Task ArtSuppliesDeliverPaintingToolsAndKeepTheExistingCrayonOrder()
    {
        await SpawnTarget("CrateFunArtSupplies");
        await Server.WaitAssertion(() =>
        {
            Assert.That(ProtoMan.Index(ArtProduct).Product, Is.EqualTo((EntProtoId) "CrayonBox"));
            var order = ProtoMan.Index(PaintingProduct);
            Assert.That(order.Product, Is.EqualTo((EntProtoId) "CrateFunArtSupplies"));
            Assert.That(order.Cost, Is.EqualTo(500));
            Assert.That(SEntMan.System<SharedContainerSystem>().TryGetContainer(STarget!.Value, "entity_storage", out var container), Is.True);
            var contents = container!.ContainedEntities.Select(uid => SEntMan.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID).ToArray();
            Assert.That(contents, Has.Length.EqualTo(11));
            Assert.That(contents.Count(id => id == "PaintCanvas"), Is.EqualTo(2));
            Assert.That(contents.Count(id => id == "PaintingFrameWood"), Is.EqualTo(2));
            foreach (var id in new[] { "CrayonBox", "Paintbrush", "PaintCanvasLandscape", "PaintCanvasSmall", "PaintCanRed", "PaintCanYellow", "PaintCanBlue" })
                Assert.That(contents.Count(value => value == id), Is.EqualTo(1), id);
        });
    }

    [TestCase("FloorCanvas", true)]
    [TestCase("Plating", false)]
    public async Task CanvasFloorKeepsDrawingsWhenCleaning(string tile, bool protectedDrawing)
    {
        await SetTile(tile);
        EntityUid decal = default;
        await Server.WaitAssertion(() =>
        {
            var coords = SEntMan.GetCoordinates(TargetCoords);
            var decals = SEntMan.System<DecalSystem>();
            var turf = SEntMan.System<TurfSystem>();
            var tileRef = turf.GetTileRef(coords);
            Assert.That(tileRef, Is.Not.Null);
            var grid = SEntMan.GetComponent<MapGridComponent>(tileRef!.Value.GridUid);
            var indices = tileRef.Value.GridIndices;
            var corner = new EntityCoordinates(tileRef.Value.GridUid,
                new System.Numerics.Vector2(indices.X, indices.Y) * grid.TileSize);
            Assert.That(decals.TryAddDecal("BrickTileDarkBox", corner, out decal, cleanable: true), Is.True);
            Assert.That(turf.GetTileRef(SEntMan.GetComponent<TransformComponent>(decal).Coordinates)!.Value.GridIndices,
                Is.EqualTo(indices));
            var used = new CleanDecalsReaction().TileReact(tileRef!.Value,
                ProtoMan.Index(Water), FixedPoint2.New(1), SEntMan, null);
            Assert.That(used > 0, Is.EqualTo(!protectedDrawing));
        });
        await RunTicks(2);
        await Server.WaitAssertion(() => Assert.That(SEntMan.EntityExists(decal), Is.EqualTo(protectedDrawing)));
    }

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

    [TestCase(false)]
    [TestCase(true)]
    public async Task FaxRejectsPaintingEvenWhenItAlsoHasPaper(bool copy)
    {
        await SpawnTarget("FaxMachineCaptain");
        await Server.WaitAssertion(() =>
        {
            var machine = STarget!.Value;
            var coords = SEntMan.GetComponent<TransformComponent>(machine).Coordinates;
            var canvas = SEntMan.SpawnEntity("PaintCanvasSmall", coords);
            // A Paper component must not let the generic fax path bypass the painting veto.
            SEntMan.EnsureComponent<PaperComponent>(canvas).Content = "Painting must stay here";
            Assert.That(SEntMan.System<ItemSlotsSystem>().TryInsert(machine, "Paper", canvas, null), Is.True);
            var fax = SEntMan.GetComponent<FaxMachineComponent>(machine);
            fax.DestinationFaxAddress = "test-receiver";
            fax.KnownFaxes["test-receiver"] = "Receiver";
            SEntMan.System<PowerReceiverSystem>().SetNeedsPower(machine, false);
        });
        await RunTicks(30);
        await Interact();
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() => Assert.That(IsUiOpen(FaxUiKey.Key), Is.True));
        await SendBui(FaxUiKey.Key, copy ? new FaxCopyMessage() : new FaxSendMessage());
        await Server.WaitAssertion(() =>
        {
            var fax = SEntMan.GetComponent<FaxMachineComponent>(STarget!.Value);
            Assert.That(fax.PrintingQueue, Is.Empty);
            Assert.That(fax.SendTimeoutRemaining, Is.Zero);
            Assert.That(fax.PaperSlot.Item, Is.Not.Null);
        });
        await CloseBui(FaxUiKey.Key);
    }

    [TestCase("A small painting", "A small painting")]
    [TestCase("   ", null)]
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

    [TestCase("coordinate")]
    [TestCase("brush")]
    [TestCase("color")]
    [TestCase("color-range")]
    [TestCase("shape")]
    [TestCase("style")]
    [TestCase("points")]
    [TestCase("length")]
    [TestCase("stroke-id")]
    [TestCase("no-tool")]
    [TestCase("stunned")]
    public async Task InvalidStrokeDoesNotMutateCanvas(string rejection)
    {
        await SpawnTarget("PaintCanvasSmall");
        await PlaceInHands("Paintbrush");
        await Interact();
        await Pair.RunUntilSynced();
        var message = rejection switch
        {
            "coordinate" => Stroke(1, 1, int.MaxValue, 0),
            "brush" => new PaintStrokeMessage(1, Color.Red, int.MaxValue, PaintBrushStyle.Round,
                PaintShape.Freehand, false, 1, new[] { new Vector2i(3, 3) }),
            "color" => new PaintStrokeMessage(1, new Color(float.PositiveInfinity, 0f, 0f), 1, PaintBrushStyle.Round,
                PaintShape.Freehand, false, 1, new[] { new Vector2i(3, 3) }),
            "color-range" => new PaintStrokeMessage(1, new Color(-1f, 0f, 0f), 1, PaintBrushStyle.Round,
                PaintShape.Freehand, false, 1, new[] { new Vector2i(3, 3) }),
            "shape" => new PaintStrokeMessage(1, Color.Red, 1, PaintBrushStyle.Round,
                (PaintShape) 255, false, 1, new[] { new Vector2i(3, 3) }),
            "style" => new PaintStrokeMessage(1, Color.Red, 1, (PaintBrushStyle) 255,
                PaintShape.Freehand, false, 1, new[] { new Vector2i(3, 3) }),
            "points" => new PaintStrokeMessage(1, Color.Red, 1, PaintBrushStyle.Round,
                PaintShape.Freehand, false, 1, Enumerable.Repeat(new Vector2i(3, 3), PaintBrush.MaxStrokePoints + 1).ToArray()),
            "length" => new PaintStrokeMessage(1, Color.Red, 1, PaintBrushStyle.Round,
                PaintShape.Freehand, false, 1, Enumerable.Range(0, 32).Select(i => new Vector2i(i % 2 * 32, 3)).ToArray()),
            "stroke-id" => new PaintStrokeMessage(1, Color.Red, 1, PaintBrushStyle.Round,
                PaintShape.Freehand, false, 0, new[] { new Vector2i(3, 3) }),
            _ => Stroke(1, 1, 3, 3),
        };
        if (rejection == "no-tool")
            await Drop();
        if (rejection == "stunned")
            await Server.WaitPost(() => SEntMan.EnsureComponent<StunnedComponent>(SPlayer));
        await SendBui(PaintUiKey.Key, message);
        await Server.WaitAssertion(() => Assert.That(
            SEntMan.GetComponent<PaintSurfaceComponent>(STarget!.Value).Image!.Pixels.All(p => p == 0), Is.True));
        if (rejection == "stunned")
            await Server.WaitPost(() => SEntMan.System<SharedStunSystem>().TryUnstun(SPlayer));
        await CloseBui(PaintUiKey.Key);
    }

    [TestCase("too-long")]
    [TestCase("control")]
    public async Task InvalidSignatureLeavesCanvasEditable(string rejection)
    {
        await SpawnTarget("PaintCanvas");
        await PlaceInHands("Paintbrush");
        await Interact();
        await Pair.RunUntilSynced();
        await SendBui(PaintUiKey.Key,
            new PaintFinishMessage(rejection == "too-long" ? new string('x', 33) : "line\nline"));
        await Server.WaitAssertion(() => Assert.That(
            SEntMan.GetComponent<PaintSurfaceComponent>(STarget!.Value).Finished, Is.False));
        await SendBui(PaintUiKey.Key, new PaintFinishMessage("Valid"));
        await Server.WaitAssertion(() => Assert.That(
            SEntMan.GetComponent<PaintSurfaceComponent>(STarget!.Value).Finished, Is.True));
        await CloseBui(PaintUiKey.Key);
    }

    [TestCase(false)]
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

    [TestCase("size")]
    [TestCase("two-blank")]
    [TestCase("two-painted")]
    [TestCase("delete-source")]
    [TestCase("delete-target")]
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

[TestFixture]
public sealed class PaintCodecTest
{
    [Test]
    public void HistoryUsesItsOwnSuffixInsteadOfTokensInTheTitle()
    {
        const string first = "pnt0123456789ab";
        const string second = "pntfedcba987654";
        foreach (var title in new[] { "pnt000000000000", "(painting pnt000000000000)", "A normal title" })
        {
            Assert.That(PaintLogToken.TryFind($"finished a painting as \"{title}\" (painting {first})", out var token), Is.True);
            Assert.That(token, Is.EqualTo(first));
            Assert.That(PaintLogToken.TryFind($"finished another painting as \"{title}\" (painting {second})", out token), Is.True);
            Assert.That(token, Is.EqualTo(second));
        }

        Assert.That(PaintLogToken.TryFind("a title pnt000000000000 without a painting suffix", out _), Is.False);
    }

    [Test]
    public void HistoryDecodeRejectsInvalidPaletteReferences()
    {
        var image = new PaintImage(1, 1, new List<Color> { Color.White }, new byte[] { 255 });
        Assert.That(PaintImageCodec.Decode(PaintImageCodec.Encode(image)), Is.Null);
    }

    [Test]
    public void HistoryRoundTripKeepsPixelsAndDoesNotAlias()
    {
        var image = new PaintImage(33, 33, Color.White);
        image.Pixels[0] = image.FindOrAddColor(Color.Red, out _);
        var decoded = PaintImageCodec.Decode(PaintImageCodec.Encode(image));
        Assert.That(decoded, Is.Not.Null);
        Assert.That(decoded!.Pixels, Is.EqualTo(image.Pixels));
        Assert.That(decoded.Palette, Is.EqualTo(image.Palette));
        decoded.Pixels[0] = 0;
        Assert.That(image.Pixels[0], Is.Not.Zero);
    }
}
