// Arcane-Start
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Content.Client.ContextMenu.UI;
using Content.Client.UserInterface.Motion;
using NUnit.Framework;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.RichText;
using Robust.Client.UserInterface.XAML.Proxy;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.UnitTesting;
using S = Content.Client.UserInterface.Motion.UiMotionSettings;

namespace Content.Tests.Client.UserInterface.Controls;

[TestFixture]
public sealed partial class UiMotionTest : RobustUnitTest
{
    public override UnitTestProject Project => UnitTestProject.Client;
    private IConfigurationManager _configuration;
    private UiMotionUIController _controller;
    private TestWindow _window;

    private sealed class TestWindow : BaseWindow;

    private sealed class TestCheckbox : CheckBox
    {
        public void Click()
        {
            base.KeyBindDown(new GUIBoundKeyEventArgs(EngineKeyFunctions.UIClick, BoundKeyState.Down, default, true, Vector2.Zero, Vector2.Zero));
            base.KeyBindUp(new GUIBoundKeyEventArgs(EngineKeyFunctions.UIClick, BoundKeyState.Up, default, true, Vector2.Zero, Vector2.Zero));
        }
    }

    private sealed class TestSwitchButton : Content.Client.UserInterface.Controls.SwitchButton
    {
        public void Click()
        {
            base.KeyBindDown(new GUIBoundKeyEventArgs(EngineKeyFunctions.UIClick, BoundKeyState.Down, default, true, Vector2.Zero, Vector2.Zero));
            base.KeyBindUp(new GUIBoundKeyEventArgs(EngineKeyFunctions.UIClick, BoundKeyState.Up, default, true, Vector2.Zero, Vector2.Zero));
        }
    }

    private sealed class TestButton : Button
    {
        public void Hover(bool value)
        {
            if (value)
                base.MouseEntered();
            else
                base.MouseExited();
        }

        public void Hold(bool value)
        {
            var state = value ? BoundKeyState.Down : BoundKeyState.Up;
            var args = new GUIBoundKeyEventArgs(EngineKeyFunctions.UIClick, state, default, true, Vector2.Zero, Vector2.Zero);
            if (value)
                base.KeyBindDown(args);
            else
                base.KeyBindUp(args);
        }
    }

    // Use compiled XAML in the unit harness, which does not start the hot-reload service.
    private sealed class CompiledXaml : IXamlProxyHelper
    {
        public bool Populate(Type type, object instance) => false;
    }

    private sealed class TestFont : Font
    {
        public override int GetAscent(float scale) => 8;
        public override int GetHeight(float scale) => 10;
        public override int GetDescent(float scale) => 2;
        public override int GetLineHeight(float scale) => 12;
        public override float DrawChar(DrawingHandleBase handle, Rune rune, Vector2 baseline, float scale, Color color, bool fallback = true) => 5;
        public override float DrawCharOutline(DrawingHandleBase handle, Rune rune, Vector2 baseline, float scale, TextOutline outline, bool fallback = true) => 5;
        public override CharMetrics? GetCharMetrics(Rune rune, float scale, bool fallback = true) => new CharMetrics(0, 8, 5, 5, 10);
    }

    protected override void OverrideIoC()
    {
        IoCManager.Register<IXamlProxyHelper, CompiledXaml>(overwrite: true);
    }

    [OneTimeSetUp]
    public void InitializeUi()
    {
        IoCManager.Resolve<IUserInterfaceManager>().InitializeTesting();
        IoCManager.Resolve<FontTagHijackHolder>().Hijack = (_, _) => new TestFont();
        _configuration = IoCManager.Resolve<IConfigurationManager>();
        _controller = new UiMotionUIController();
        IoCManager.InjectDependencies(_controller);
        _controller.Initialize();
    }

    private void Default<T>(CVarDef<T> setting) where T : notnull
    {
        if (!_configuration.IsCVarRegistered(setting.Name))
            _configuration.RegisterCVar(setting.Name, setting.DefaultValue, setting.Flags);
        _configuration.SetCVar(setting, setting.DefaultValue);
    }

    [SetUp]
    public void ResetSettings()
    {
        Default(S.Enabled);
        Default(S.Speed);
        Default(S.AnimateClosing);
        Default(S.Windows);
        Default(S.WindowEnterMs);
        Default(S.WindowTravel);
        Default(S.Buttons);
        Default(S.ButtonEnterMs);
        Default(S.ButtonIntervalMs);
        Default(S.ButtonJitterMs);
        Default(S.ButtonMaxDelayMs);
        Default(S.ButtonOrder);
        Default(S.ButtonHover);
        Default(S.ButtonPress);
        Default(S.ButtonHoverIntensity);
        Default(S.ButtonPressIntensity);
        Default(S.Checkboxes);
        Default(S.CheckboxMs);
        Default(S.CheckboxIntensity);
        Default(S.MenuOpening);
        Default(S.MenuSequence);
        Default(S.MenuFade);
        Default(S.MenuSpring);
        Default(S.MenuEnterMs);
        Default(S.MenuExitMs);
        Default(S.MenuTravel);
        Default(S.MenuSequenceMs);
    }

    [TearDown]
    public void CloseWindow()
    {
        _window?.Close();
        _window = null;
        _controller.FrameUpdate(new FrameEventArgs(0));
    }

    [TestCase(float.NaN, 1f)]
    [TestCase(float.PositiveInfinity, 1f)]
    [TestCase(float.NegativeInfinity, 1f)]
    [TestCase(-1f, 0.25f)]
    [TestCase(100f, 3f)]
    [TestCase(1.5f, 1.5f)]
    public void SpeedIsFiniteAndBounded(float input, float expected)
    {
        Assert.That(S.NormalizeSpeed(input), Is.EqualTo(expected));
    }

    [Test]
    public void RandomSequenceIsBoundedAndOrderedJitterCannotReverseNeighbors()
    {
        for (var i = 0; i < 100; i++)
        {
            Assert.That(UiMotionSequence.Delay(i, 3, 25, 150, 180, 1), Is.InRange(0, 180));
            Assert.That(UiMotionSequence.Delay(i, 0, 25, 150, 180, 1), Is.Zero);
            var latest = UiMotionSequence.Delay(i, 2, 25, 150, 180, 1);
            var earliestNext = UiMotionSequence.Delay(i + 1, 2, 25, 150, 180, 0);
            Assert.That(latest, Is.LessThanOrEqualTo(earliestNext));
        }
    }

    [Test]
    public void TextRevealPreservesMarkupAndCompleteGraphemes()
    {
        var message = FormattedMessage.FromMarkupOrThrow("[bold]A\u0301👨‍👩‍👧‍👦[/bold][color=red]привет[/color]");
        var reveal = new UiTextReveal(message);
        Assert.That(reveal.Count, Is.EqualTo(8));
        var first = reveal.Reveal(1);
        Assert.That(first.ToString(), Is.EqualTo(message.ToString()), "Hidden text must still participate in layout.");
        var plain = first.Nodes.Where(node => node.IsPlainText).ToArray();
        Assert.That(plain[0].Value.StringValue, Is.EqualTo("A\u0301"));
        Assert.That(plain[1].Value.StringValue, Is.EqualTo("👨‍👩‍👧‍👦"));
        Assert.That(first.Nodes.Any(node => node.Name == "bold"), Is.True);
        Assert.That(reveal.Reveal(reveal.Count), Is.EqualTo(message));
        Assert.That(reveal.Reveal(-1).ToString(), Is.EqualTo(message.ToString()));
    }

    [Test]
    public void TextRevealHasPunctuationPauseAndHardDeadline()
    {
        var reveal = new UiTextReveal(FormattedMessage.FromUnformatted("A.B"));
        Assert.That(reveal.VisibleCount(0, 10, 0.5f, 2), Is.Zero);
        Assert.That(reveal.VisibleCount(0.4f, 10, 0.5f, 2), Is.EqualTo(2));
        Assert.That(reveal.VisibleCount(0.4f, 10, 0, 2), Is.EqualTo(3));
        Assert.That(reveal.VisibleCount(0.2f, 10, 10, 0.2f), Is.EqualTo(3));
        Assert.That(new UiTextReveal(FormattedMessage.Empty).VisibleCount(0, 10, 0, 2), Is.Zero);
    }

    [Test]
    public void RevealKeepsMultilineLabelMeasurements()
    {
        var reveal = new UiTextReveal(FormattedMessage.FromMarkupOrThrow("[bold]A long sentence[/bold] with multiple words and lines."));
        var label = new RichTextLabel { MaxWidth = 80 };
        label.Stylesheet = new Stylesheet(new[]
        {
            new StyleRule(new SelectorElement(typeof(RichTextLabel), null, null, null),
                new[] { new StyleProperty("font", new TestFont()) })
        });
        label.SetMessage(reveal.Reveal(reveal.Count));
        label.Measure(new Vector2(80, 1000));
        var fullSize = label.DesiredSize;
        Assert.That(fullSize.Y, Is.GreaterThan(10), "The case must actually wrap onto multiple lines.");
        foreach (var count in new[] { 0, 1, 8, reveal.Count - 1 })
        {
            label.SetMessage(reveal.Reveal(count));
            label.Measure(new Vector2(80, 1000));
            Assert.That(label.DesiredSize, Is.EqualTo(fullSize));
        }
    }

    [Test]
    public void ContextMenusCloseSubmenusAndCanBeReused()
    {
        var context = new ContextMenuUIController();
        IoCManager.InjectDependencies(context);
        context.Setup();
        try
        {
            var root = context.RootMenu;
            var element = new ContextMenuElement();
            context.AddElement(root, element);
            var sub = new ContextMenuPopup(context, element);
            root.Open();
            context.OpenSubMenu(element);
            Assert.That(context.Menus.Count, Is.EqualTo(2));
            Assert.That(sub.Visible, Is.True);
            var closed = 0;
            context.OnContextClosed += () => closed++;
            context.Close();
            Assert.That(closed, Is.EqualTo(1));
            Assert.That(root.Visible, Is.False);
            Assert.That(sub.Visible, Is.False);
            Assert.That(sub.IsInsideTree, Is.False);
            Assert.That(context.Menus.Count, Is.EqualTo(1));
            Assert.That(root.MenuBody.ChildCount, Is.Zero);
            root.Open();
            Assert.That(root.Visible, Is.True);
        }
        finally
        {
            context.Shutdown();
        }
        Assert.That(context.Menus, Is.Empty);
    }

    [Test]
    public void OrphaningAnOpenSubmenuRemovesItFromLogicalStack()
    {
        var context = new ContextMenuUIController();
        IoCManager.InjectDependencies(context);
        context.Setup();
        try
        {
            var element = new ContextMenuElement();
            context.AddElement(context.RootMenu, element);
            var sub = new ContextMenuPopup(context, element);
            context.RootMenu.Open();
            context.OpenSubMenu(element);
            sub.Orphan();
            Assert.That(context.Menus.Count, Is.EqualTo(1));
        }
        finally
        {
            context.Shutdown();
        }
    }

    [Test]
    public void MenuPreferencesAreIndependentAndInvalidValuesAreNormalized()
    {
        _configuration.SetCVar(S.MenuEnterMs, float.NaN);
        _configuration.SetCVar(S.MenuTravel, -10f);
        _configuration.SetCVar(S.MenuOpening, false);
        _configuration.SetCVar(S.MenuSequence, false);
        var profile = S.MenuProfile(_configuration);
        Assert.That(profile.EnterDuration.TotalMilliseconds, Is.EqualTo(350));
        Assert.That(profile.Travel, Is.Zero);
        Assert.That(profile.Opening, Is.False);
        Assert.That(profile.Sequence, Is.False);
        Assert.That(profile.Fade, Is.True);
    }

    [Test]
    public void WindowCloseIsImmediateAndReopenDoesNotRetainOpacity()
    {
        _window = new TestWindow();
        var original = new Color(0.4f, 0.5f, 0.6f, 0.7f);
        _window.Modulate = original;
        _window.Open();
        _controller.FrameUpdate(new FrameEventArgs(0.02f));
        Assert.That(_window.Modulate.A, Is.LessThan(original.A));
        _window.Close();
        Assert.That(_window.IsOpen, Is.False);
        Assert.That(_window.Modulate, Is.EqualTo(original));
        _window.Open();
        _controller.FrameUpdate(new FrameEventArgs(2));
        Assert.That(_window.Modulate, Is.EqualTo(original));
    }

    [Test]
    public void ButtonsAppearInSequenceAndRemainInteractive()
    {
        _configuration.SetCVar(S.Windows, false);
        _configuration.SetCVar(S.ButtonOrder, 1);
        _configuration.SetCVar(S.ButtonIntervalMs, 100f);
        _window = new TestWindow();
        var first = new Button();
        var second = new Button();
        _window.AddChild(first);
        _window.AddChild(second);
        _window.Open();
        _controller.FrameUpdate(new FrameEventArgs(0.05f));
        Assert.That(first.Modulate.A, Is.GreaterThan(second.Modulate.A));
        Assert.That(second.VisibleInTree, Is.True);
        Assert.That(second.Disabled, Is.False);
        Assert.That(second.MouseFilter, Is.EqualTo(Control.MouseFilterMode.Stop));
        _configuration.SetCVar(S.Buttons, false);
        _controller.FrameUpdate(new FrameEventArgs(0));
        Assert.That(first.Modulate, Is.EqualTo(Color.White));
        Assert.That(second.Modulate, Is.EqualTo(Color.White));
    }

    [Test]
    public void HideAndRemoveRestoreTintsAndGlobalDisableSnapsImmediately()
    {
        _window = new TestWindow();
        var button = new Button();
        _window.AddChild(button);
        _window.Open();
        _controller.FrameUpdate(new FrameEventArgs(0));
        button.Visible = false;
        Assert.That(button.Modulate, Is.EqualTo(Color.White));
        button.Visible = true;
        _controller.FrameUpdate(new FrameEventArgs(0));
        button.Orphan();
        Assert.That(button.Modulate, Is.EqualTo(Color.White));
        _configuration.SetCVar(S.Enabled, false);
        _controller.FrameUpdate(new FrameEventArgs(0));
        Assert.That(_window.Modulate, Is.EqualTo(Color.White));
    }

    [Test]
    public void OwnerTintChangesSurviveCompletion()
    {
        _window = new TestWindow();
        _window.Open();
        _controller.FrameUpdate(new FrameEventArgs(0));
        var updated = Color.Red.WithAlpha(0.4f);
        _window.Modulate = updated;
        _controller.FrameUpdate(new FrameEventArgs(2));
        Assert.That(_window.Modulate, Is.EqualTo(updated));
    }

    [Test]
    public void CheckboxStateChangesImmediatelyAndFeedbackCanBeDisabledSeparately()
    {
        _window = new TestWindow();
        var checkbox = new TestCheckbox { MuteSounds = true, Mode = BaseButton.ActionMode.Press };
        _window.AddChild(checkbox);
        _window.Open();
        _controller.FrameUpdate(new FrameEventArgs(2));
        checkbox.Click();
        Assert.That(checkbox.Pressed, Is.True);
        Assert.That(checkbox.TextureRect.Modulate.A, Is.LessThan(1));
        _configuration.SetCVar(S.Checkboxes, false);
        _controller.FrameUpdate(new FrameEventArgs(0));
        Assert.That(checkbox.TextureRect.Modulate, Is.EqualTo(Color.White));
        checkbox.Click();
        Assert.That(checkbox.Pressed, Is.False);
        Assert.That(checkbox.TextureRect.Modulate, Is.EqualTo(Color.White));
    }

    [Test]
    public void SwitchStateChangesImmediatelyAndUsesToggleFeedback()
    {
        _window = new TestWindow();
        var toggle = new TestSwitchButton { MuteSounds = true };
        _window.AddChild(toggle);
        _window.Open();
        _controller.FrameUpdate(new FrameEventArgs(2));
        toggle.Click();
        Assert.That(toggle.Pressed, Is.True);
        Assert.That(toggle.ThumbFill.Modulate.A, Is.LessThan(1));
        _configuration.SetCVar(S.Checkboxes, false);
        _controller.FrameUpdate(new FrameEventArgs(0));
        Assert.That(toggle.ThumbFill.Modulate, Is.EqualTo(Color.White));
    }

    [Test]
    public void ButtonHoverAndPressFeedbackPreserveInputAndRestoreTint()
    {
        _window = new TestWindow();
        var button = new TestButton { MuteSounds = true };
        _window.AddChild(button);
        _window.Open();
        _controller.FrameUpdate(new FrameEventArgs(2));
        var filter = button.MouseFilter;

        button.Hover(true);
        _controller.FrameUpdate(new FrameEventArgs(0.03f));
        Assert.That(button.Modulate.B, Is.GreaterThan(button.Modulate.R));
        Assert.That(button.MouseFilter, Is.EqualTo(filter));

        button.Hold(true);
        _controller.FrameUpdate(new FrameEventArgs(0.03f));
        Assert.That(button.Modulate.R, Is.LessThan(1));
        button.Hold(false);
        button.Hover(false);
        _controller.FrameUpdate(new FrameEventArgs(1));
        Assert.That(button.Modulate, Is.EqualTo(Color.White));
        Assert.That(button.MouseFilter, Is.EqualTo(filter));
    }

    [Test]
    public void AnimationSettingsRoundTripThroughConfiguration()
    {
        _configuration.SetCVar(S.Speed, 2.25f);
        _configuration.SetCVar(S.Buttons, false);
        using var stream = new MemoryStream();
        _configuration.SaveToTomlStream(stream, new[] { S.Speed.Name, S.Buttons.Name });
        _configuration.SetCVar(S.Speed, 1f);
        _configuration.SetCVar(S.Buttons, true);
        stream.Position = 0;
        _configuration.LoadFromTomlStream(stream);
        Assert.That(_configuration.GetCVar(S.Speed), Is.EqualTo(2.25f));
        Assert.That(_configuration.GetCVar(S.Buttons), Is.False);
    }
}
// Arcane-End
