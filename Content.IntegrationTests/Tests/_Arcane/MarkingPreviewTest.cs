using System.Linq;
using Content.Client.Humanoid;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Body;
using Content.Shared.CCVar;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class MarkingPreviewTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };
    private static readonly ProtoId<OrganCategoryPrototype> Tail = "Tail";
    private static readonly ProtoId<MarkingPrototype> Still = "LizardTailSmooth";
    private static readonly ProtoId<MarkingPrototype> Animated = "LizardTailSmoothAnimated";

    [Test]
    public async Task AnimationAndReducedMotionOnlyChangeDisplayCopies()
    {
        MarkingsViewModel model = null!;
        MarkingPicker picker = null!;
        CheckBox toggle = null!;
        await Client.WaitAssertion(() =>
        {
            var manager = Client.ResolveDependency<MarkingManager>();
            var cfg = Client.ResolveDependency<IConfigurationManager>();
            cfg.SetCVar(CCVars.ReducedMotion, false);
            model = new MarkingsViewModel
            {
                OrganProfileData = manager.GetProfileData("Reptilian", Sex.Male, Color.Green, Color.Black)
                    .Where(pair => pair.Key == Tail).ToDictionary(),
                OrganData = manager.GetMarkingData("Reptilian").Where(pair => pair.Key == Tail).ToDictionary(),
                Markings = new()
                {
                    [Tail] = new() { [HumanoidVisualLayers.Tail] = [new Marking(Still, new[] {Color.Red, Color.Blue})] },
                },
            };
        });
        var edits = 0;
        await Client.WaitPost(() =>
        {
            model.MarkingsChanged += (_, _) => edits++;
            model.MarkingsReset += () => edits++;
            picker = new MarkingPicker { MinSize = new Vector2(600, 600) };
            picker.SetModel(model);
            Client.ResolveDependency<IUserInterfaceManager>().StateRoot.AddChild(picker);
            toggle = picker.FindControl<CheckBox>("AnimatedPreview");
        });
        try
        {
            await Pair.ReallyBeIdle(2);
            await Click(toggle);
            await Client.WaitAssertion(() =>
            {
                Assert.That(model.AnimatePreview, Is.True);
                var preview = model.GetPreviewMarkings()[Tail][HumanoidVisualLayers.Tail][0];
                Assert.That(preview.MarkingId, Is.EqualTo(Animated));
                Assert.That(preview.MarkingColors, Is.EqualTo(new[] {Color.Red, Color.Blue}));
                ((List<Color>) preview.MarkingColors)[0] = Color.Yellow;
                Assert.That(model.Markings[Tail][HumanoidVisualLayers.Tail][0].MarkingId, Is.EqualTo(Still));
                Assert.That(model.Markings[Tail][HumanoidVisualLayers.Tail][0].MarkingColors[0], Is.EqualTo(Color.Red));

                var cfg = Client.ResolveDependency<IConfigurationManager>();
                cfg.SetCVar(CCVars.ReducedMotion, true);
                Assert.That(toggle.Disabled, Is.True);
                Assert.That(model.AnimatePreview, Is.False);
                Assert.That(model.GetPreviewMarkings()[Tail][HumanoidVisualLayers.Tail][0].MarkingId, Is.EqualTo(Still));
                cfg.SetCVar(CCVars.ReducedMotion, false);
                Assert.That(model.AnimatePreview, Is.True);
                Assert.That(edits, Is.Zero, "Preview and accessibility toggles must not edit the saved markings.");

                model.Markings[Tail][HumanoidVisualLayers.Tail][0] = new Marking(Animated, new[] {Color.Red, Color.Blue});
                var manager = Client.ResolveDependency<MarkingManager>();
                var row = new LayerMarkingItem(model, Tail, HumanoidVisualLayers.Tail, manager.MarkingsByLayer(HumanoidVisualLayers.Tail)[Still], true);
                Assert.That(row.FindControl<Button>("SelectButton").Pressed, Is.True,
                    "A saved animated variant must remain selected on its still catalogue row.");
            });
        }
        finally
        {
            await Client.WaitPost(() => picker.Parent?.RemoveChild(picker));
        }
    }

    private async Task Click(Control control)
    {
        GUIBoundKeyEventArgs down = null!;
        GUIBoundKeyEventArgs up = null!;
        await Client.WaitPost(() =>
        {
            var point = new ScreenCoordinates(control.GlobalPixelPosition + control.PixelSize / 2, control.Window?.Id ?? default);
            var relative = point.Position / control.UIScale - control.GlobalPosition;
            var pixels = point.Position - control.GlobalPixelPosition;
            down = new GUIBoundKeyEventArgs(EngineKeyFunctions.UIClick, BoundKeyState.Down, point, default, relative, pixels);
            up = new GUIBoundKeyEventArgs(EngineKeyFunctions.UIClick, BoundKeyState.Up, point, default, relative, pixels);
        });
        await Client.DoGuiEvent(control, down);
        await Pair.ReallyBeIdle(1);
        await Client.DoGuiEvent(control, up);
        await Pair.ReallyBeIdle(1);
    }
}
