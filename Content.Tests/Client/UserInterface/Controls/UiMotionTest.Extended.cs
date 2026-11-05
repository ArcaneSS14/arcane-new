// Arcane-Start
using System;
using System.Linq;
using System.Numerics;
using Content.Client.ContextMenu.UI;
using Content.Client.Chemistry.UI;
using Content.Client.UserInterface.Motion;
using Content.Shared.Chemistry;
using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.ContentPack;
using S = Content.Client.UserInterface.Motion.UiMotionSettings;

namespace Content.Tests.Client.UserInterface.Controls;

public sealed partial class UiMotionTest
{
    [Test]
    public void RecreatedReagentButtonsOnlyFadeWithNewRows()
    {
        _window = new TestWindow();
        var list = new BoxContainer();
        _window.AddChild(list);
        _window.Open();
        _controller.FrameUpdate(new FrameEventArgs(2));

        var oldRow = new PanelContainer();
        var oldButton = new ReagentButton("1", ChemMasterReagentAmount.U1, default, true, "test");
        oldRow.AddChild(oldButton);
        list.AddChild(oldRow);
        _controller.AnimateListItem(oldRow);
        _controller.FrameUpdate(new FrameEventArgs(2));

        // ChemMaster rebuilds an existing row when only its amount changes.
        oldRow.Orphan();
        var updatedRow = new PanelContainer();
        var updatedButton = new ReagentButton("1", ChemMasterReagentAmount.U1, default, true, "test");
        updatedRow.AddChild(updatedButton);
        list.AddChild(updatedRow);
        _controller.FrameUpdate(new FrameEventArgs(0.02f));
        Assert.That(updatedRow.Modulate, Is.EqualTo(Color.White));
        Assert.That(updatedButton.Modulate, Is.EqualTo(Color.White));
        Assert.That(updatedButton.Disabled, Is.False);
        Assert.That(updatedButton.MouseFilter, Is.EqualTo(Control.MouseFilterMode.Stop));

        var newRow = new PanelContainer();
        var newButton = new ReagentButton("1", ChemMasterReagentAmount.U1, default, true, "test");
        newRow.AddChild(newButton);
        list.AddChild(newRow);
        _controller.AnimateListItem(newRow);
        _controller.FrameUpdate(new FrameEventArgs(0.02f));
        Assert.That(newRow.Modulate.A, Is.InRange(0.01f, 0.99f));
        Assert.That(newButton.Modulate, Is.EqualTo(Color.White));
        Assert.That(newButton.Disabled, Is.False);
        Assert.That(newButton.MouseFilter, Is.EqualTo(Control.MouseFilterMode.Stop));

        var ordinary = new Button();
        list.AddChild(ordinary);
        _controller.FrameUpdate(new FrameEventArgs(0.02f));
        Assert.That(ordinary.Modulate.A, Is.InRange(0.01f, 0.99f));
    }

    private bool _slotPrototypesInitialized;
    private sealed class TestSlot : Content.Client.UserInterface.Controls.SlotControl
    {
        public void Step(float delta) => base.FrameUpdate(new FrameEventArgs(delta));
        public bool Contains(Vector2 position) => base.HasPoint(position);
    }

    private void PrepareSlotPrototypes()
    {
        if (_slotPrototypesInitialized)
            return;
        var resources = new MemoryContentRoot();
        resources.AddOrUpdateFile(new ResPath("Shaders/Internal/default-sprite.swsl"), "void fragment() { COLOR = vec4(1.0); }");
        // One opaque pixel is sufficient: these tests exercise control ownership, not asset rendering.
        var pixel = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGP4////fwAJ+wP9KobjigAAAABJRU5ErkJggg==");
        resources.AddOrUpdateFile(new ResPath("Textures/Interface/Default/slot_highlight.png"), pixel);
        resources.AddOrUpdateFile(new ResPath("Textures/Interface/Default/blocked.png"), pixel);
        IoCManager.Resolve<IResourceManager>().AddRoot(ResPath.Root, resources);
        IoCManager.Resolve<ISerializationManager>().Initialize();
        var prototypes = IoCManager.Resolve<IPrototypeManager>();
        prototypes.Initialize();
        prototypes.LoadString("""
            - type: shader
              id: CooldownAnimation
              kind: canvas
            - type: uiTheme
              id: Default
            - type: entity
              id: UiMotionTestItem
              components:
              - type: Sprite
            """);
        prototypes.ResolveResults();
        IoCManager.Resolve<IUserInterfaceManager>().SetDefaultTheme("Default");
        _slotPrototypesInitialized = true;
    }

    [Test]
    public void SlotClearsLogicalEntityImmediatelyWithoutAddingLiveExitSprite()
    {
        PrepareSlotPrototypes();
        var entities = IoCManager.Resolve<IEntityManager>();
        var item = entities.SpawnEntity("UiMotionTestItem", MapCoordinates.Nullspace);
        _window = new TestWindow();
        var slot = new TestSlot();
        _window.AddChild(slot);
        _window.Open();
        slot.Measure(new Vector2(64));
        slot.Arrange(UIBox2.FromDimensions(Vector2.Zero, new Vector2(64)));
        var children = slot.ChildCount;
        try
        {
            slot.SetEntity(item);
            slot.Step(0.02f);
            Assert.That(slot.Entity, Is.EqualTo(item));
            Assert.That(slot.ButtonRect.MouseFilter, Is.EqualTo(Control.MouseFilterMode.Stop));
            Assert.That(slot.Contains(new Vector2(32)), Is.True);
            Assert.That(slot.Size, Is.EqualTo(new Vector2(64)));
            slot.SetEntity(null);
            Assert.That(slot.Entity, Is.Null);
            Assert.That(slot.ChildCount, Is.EqualTo(children));
            entities.DeleteEntity(item);
            slot.Step(0.01f);
            Assert.That(slot.ChildCount, Is.EqualTo(children));
        }
        finally
        {
            if (entities.EntityExists(item))
                entities.DeleteEntity(item);
        }
    }

    [Test]
    public void SlotPrototypeChangesAreBoundedAndDisableRestoresHighlight()
    {
        PrepareSlotPrototypes();
        _window = new TestWindow();
        var slot = new TestSlot();
        _window.AddChild(slot);
        _window.Open();
        var children = slot.ChildCount;
        for (var i = 0; i < 5; i++)
        {
            slot.SetPrototype("UiMotionTestItem", false);
            slot.SetPrototype(null, false);
            Assert.That(slot.ChildCount, Is.EqualTo(children));
        }
        slot.Highlight = true;
        _configuration.SetCVar(S.Enabled, false);
        slot.Step(0);
        Assert.That(slot.ChildCount, Is.EqualTo(children));
        Assert.That(slot.Highlight, Is.True);
        Assert.That(slot.HighlightRect.Visible, Is.True);
        Assert.That(slot.HighlightRect.Modulate, Is.EqualTo(Color.White));
        slot.Orphan();
        Assert.That(slot.ChildCount, Is.EqualTo(children));
    }

    [TestCase("🇺🇦", 1)]
    [TestCase("👍🏽", 1)]
    [TestCase("1️⃣", 1)]
    [TestCase("\r\n", 1)]
    [TestCase("🇺🇦🇬🇧🇫", 3)]
    [TestCase("👨‍👩‍👧‍👦", 1)]
    [TestCase("A\u0301", 1)]
    [TestCase("🏴\U000E0067\U000E0062\U000E007F", 1)]
    public void TextRevealGroupsEmojiSequencesWithoutStringInfo(string text, int expected)
    {
        var reveal = new UiTextReveal(FormattedMessage.FromUnformatted(text));
        Assert.That(reveal.Count, Is.EqualTo(expected));
        Assert.That(reveal.Reveal(0).ToString(), Is.EqualTo(text));
        Assert.That(reveal.Reveal(expected).ToString(), Is.EqualTo(text));
    }

    [TestCase("A\u0301", 2, "A\u0301")]
    [TestCase("👨‍👩‍👧‍👦", 2, "👨‍👩‍👧‍👦")]
    [TestCase("🇺🇦", 2, "🇺🇦")]
    [TestCase("\r\n", 2, "\r\n")]
    [TestCase("\u1100\u1161", 3, "\u1100")]
    [TestCase("\u0915\u094d\u0937", 3, "\u0915\u094d")]
    public void TextRevealDocumentsSupportedClustersAndKnownSplits(string cluster, int expectedCount, string firstVisible)
    {
        var text = cluster + "X";
        var reveal = new UiTextReveal(FormattedMessage.FromUnformatted(text));
        Assert.That(reveal.Count, Is.EqualTo(expectedCount));
        var intermediate = reveal.Reveal(1);
        Assert.That(intermediate.ToString(), Is.EqualTo(text));
        Assert.That(intermediate.Nodes.First(node => node.IsPlainText).Value.StringValue, Is.EqualTo(firstVisible));
    }

    [Test]
    public void TextRevealKeepsCombiningClusterAcrossMarkupNodes()
    {
        var message = FormattedMessage.FromMarkupOrThrow("[bold]A[/bold]\u0301X");
        var reveal = new UiTextReveal(message);
        Assert.That(reveal.Count, Is.EqualTo(2));
        var intermediate = reveal.Reveal(1);
        Assert.That(intermediate.ToString(), Is.EqualTo(message.ToString()));
        var textNodes = intermediate.Nodes.Where(node => node.IsPlainText).ToArray();
        Assert.That(textNodes.Select(node => node.Value.StringValue), Is.EqualTo(new[] { "A", "\u0301", "X" }));
        var hidden = intermediate.Nodes.ToList().FindIndex(node => node == textNodes[2]);
        Assert.That(intermediate.Nodes[hidden - 1].Value.ColorValue, Is.EqualTo(Color.Transparent));
    }

    [SetUp]
    public void ResetExtendedSettings()
    {
        Default(S.Reduce);
        Default(S.Popups);
        Default(S.PopupsMs);
        Default(S.Dropdowns);
        Default(S.DropdownEnterMs);
        Default(S.DropdownTravel);
        Default(S.DropdownItemMs);
        Default(S.Slots);
        Default(S.SlotEnterMs);
        Default(S.SlotExitMs);
        Default(S.SlotHighlight);
        Default(S.Actions);
        Default(S.ActionsEnterMs);
        Default(S.ActionsCooldownPulse);
        Default(S.Alerts);
        Default(S.AlertsEnterMs);
        Default(S.AlertsTravel);
        Default(S.AlertsSeverityPulse);
        Default(S.Chat);
        Default(S.ChatMs);
        Default(S.Notifications);
        Default(S.NotificationsMs);
        Default(S.Progress);
        Default(S.ProgressMs);
        Default(S.Radial);
        Default(S.RadialEnterMs);
        Default(S.RadialHover);
        Default(S.RadialHoverScale);
        Default(S.Lists);
        Default(S.ListsEnterMs);
        Default(S.ListsMaxStagger);
        Default(S.Tabs);
        Default(S.TabsEnterMs);
        Default(S.Votes);
        Default(S.VotesEnterMs);
        Default(S.VotesTravel);
    }

    [Test]
    public void PopupAndTooltipFadeOnceAndCloseImmediately()
    {
        var ui = IoCManager.Resolve<IUserInterfaceManager>();
        var popup = new Popup();
        var button = new Button();
        popup.AddChild(button);
        ui.ModalRoot.AddChild(popup);
        var tooltip = new PanelContainer();
        var tooltipButton = new Button();
        tooltip.AddChild(tooltipButton);
        ui.PopupRoot.AddChild(tooltip);
        try
        {
            popup.Open();
            _controller.FrameUpdate(new FrameEventArgs(0.02f));
            Assert.That(popup.Modulate.A, Is.InRange(0.01f, 0.99f));
            Assert.That(tooltip.Modulate.A, Is.InRange(0.01f, 0.99f));
            Assert.That(button.Modulate, Is.EqualTo(Color.White));
            Assert.That(tooltipButton.Modulate, Is.EqualTo(Color.White));
            popup.Close();
            Assert.That(popup.Visible, Is.False);
            Assert.That(popup.Modulate, Is.EqualTo(Color.White));
            _configuration.SetCVar(S.Popups, false);
            _controller.FrameUpdate(new FrameEventArgs(0));
            Assert.That(tooltip.Modulate, Is.EqualTo(Color.White));
        }
        finally
        {
            popup.Close();
            popup.Orphan();
            tooltip.Orphan();
        }
    }

    [Test]
    public void ContextMenuRetainsItsOwnTransition()
    {
        var context = new ContextMenuUIController();
        IoCManager.InjectDependencies(context);
        context.Setup();
        try
        {
            context.RootMenu.Open();
            _controller.FrameUpdate(new FrameEventArgs(0));
            Assert.That(context.RootMenu.Modulate, Is.EqualTo(Color.White));
        }
        finally
        {
            context.Shutdown();
        }
    }

    [Test]
    public void DropdownPopupAnimatesItsSurfaceAndItems()
    {
        var ui = IoCManager.Resolve<IUserInterfaceManager>();
        var popup = new Popup { StyleClasses = { OptionButton.StyleClassPopup } };
        var first = new Button();
        var second = new Button();
        popup.AddChild(first);
        popup.AddChild(second);
        ui.ModalRoot.AddChild(popup);
        try
        {
            popup.Open();
            _controller.FrameUpdate(new FrameEventArgs(0.02f));
            Assert.That(popup.Modulate.A, Is.InRange(0.01f, 0.99f));
            Assert.That(first.Modulate.A, Is.GreaterThan(second.Modulate.A));
            Assert.That(first.MouseFilter, Is.EqualTo(Control.MouseFilterMode.Stop));
            Assert.That(second.MouseFilter, Is.EqualTo(Control.MouseFilterMode.Stop));
        }
        finally
        {
            popup.Close();
            popup.Orphan();
        }
    }

    [Test]
    public void IndependentContainersStartTheirSequencesTogether()
    {
        _configuration.SetCVar(S.ButtonOrder, 1);
        _configuration.SetCVar(S.ButtonIntervalMs, 100f);
        _window = new TestWindow();
        var left = new BoxContainer();
        var right = new BoxContainer();
        var a = new Button();
        var b = new Button();
        var c = new Button();
        var d = new Button();
        left.AddChild(a);
        left.AddChild(b);
        right.AddChild(c);
        right.AddChild(d);
        _window.AddChild(left);
        _window.AddChild(right);
        _window.Open();
        _controller.FrameUpdate(new FrameEventArgs(0.05f));
        Assert.That(a.Modulate, Is.EqualTo(c.Modulate));
        Assert.That(b.Modulate, Is.EqualTo(d.Modulate));
        Assert.That(a.Modulate.A, Is.GreaterThan(b.Modulate.A));
    }

    [Test]
    public void StaggeredTransitionWaitsForLastItemFade()
    {
        var transition = new UiMotionTransition(_configuration);
        var profile = new UiMotionProfile(TimeSpan.FromMilliseconds(200), TimeSpan.Zero, 0, false)
        {
            EnterEffectDuration = TimeSpan.FromMilliseconds(150),
            Sequence = true,
            SequenceSeconds = 0.1f
        };
        transition.Start(profile, true, 0, 0);
        transition.Advance(0.15f);
        Assert.That(transition.Finished, Is.False);
        Assert.That(transition.GetStaggeredOpacity(3, 4), Is.LessThan(1));
        transition.Advance(0.1f);
        Assert.That(transition.Finished, Is.True);
        Assert.That(transition.GetStaggeredOpacity(3, 4), Is.EqualTo(1));
    }

    [Test]
    public void SwitchingTabsAnimatesOnlyTheNewPage()
    {
        _window = new TestWindow();
        var tabs = new TabContainer();
        var first = new Label();
        var second = new Label();
        tabs.AddChild(first);
        tabs.AddChild(second);
        _window.AddChild(tabs);
        _window.Open();
        _controller.FrameUpdate(new FrameEventArgs(2));
        Assert.That(first.Modulate, Is.EqualTo(Color.White));

        tabs.CurrentTab = 1;
        _controller.FrameUpdate(new FrameEventArgs(0.02f));
        Assert.That(first.Modulate, Is.EqualTo(Color.White));
        Assert.That(second.Modulate.A, Is.InRange(0.01f, 0.99f));
    }

    [Test]
    public void ReducedMotionRemovesDelayScaleAndCheckboxPulse()
    {
        _configuration.SetCVar(S.Reduce, true);
        _configuration.SetCVar(S.Speed, 0.25f);
        _configuration.SetCVar(S.ButtonOrder, 1);
        _configuration.SetCVar(S.ButtonIntervalMs, 100f);
        var profile = S.Resolve(_configuration, UiMotionProfile.Expressive);
        Assert.That(profile.Travel, Is.Zero);
        Assert.That(profile.Overshoot, Is.False);
        Assert.That(profile.Sequence, Is.False);
        Assert.That(profile.EnterDuration.TotalMilliseconds, Is.LessThanOrEqualTo(100));
        Assert.That(S.PlaybackSpeed(_configuration), Is.EqualTo(1));
        _window = new TestWindow();
        var first = new Button();
        var second = new Button();
        var checkbox = new TestCheckbox { MuteSounds = true, Mode = BaseButton.ActionMode.Press };
        _window.AddChild(first);
        _window.AddChild(second);
        _window.AddChild(checkbox);
        _window.Open();
        _controller.FrameUpdate(new FrameEventArgs(0.02f));
        Assert.That(first.Modulate, Is.EqualTo(second.Modulate));
        checkbox.Click();
        Assert.That(checkbox.Pressed, Is.True);
        Assert.That(checkbox.TextureRect.Modulate, Is.EqualTo(Color.White));
        _controller.FrameUpdate(new FrameEventArgs(0.1f));
        Assert.That(first.Modulate, Is.EqualTo(Color.White));
    }

    [Test]
    public void VisualFadeIsMonotoneAndRestoresOwnersTintAndScale()
    {
        _window = new TestWindow();
        var sprite = new SpriteView { Scale = new Vector2(2), Modulate = Color.Red.WithAlpha(0.7f) };
        _window.AddChild(sprite);
        _window.Open();
        var motion = new UiMotionVisual(sprite, _configuration, S.Slots);
        motion.Start(100, shrink: 0.08f);
        Assert.That(sprite.Scale.X, Is.LessThan(2));
        var opacity = sprite.Modulate.A;
        for (var i = 0; i < 12; i++)
        {
            motion.Advance(0.01f);
            Assert.That(sprite.Modulate.A, Is.GreaterThanOrEqualTo(opacity));
            opacity = sprite.Modulate.A;
        }
        Assert.That(sprite.Scale, Is.EqualTo(new Vector2(2)));
        Assert.That(sprite.Modulate, Is.EqualTo(Color.Red.WithAlpha(0.7f)));
        motion.Start(100);
        sprite.Modulate = Color.Blue;
        motion.Advance(1);
        Assert.That(sprite.Modulate, Is.EqualTo(Color.Blue));
        motion.Start(100);
        _configuration.SetCVar(S.Slots, false);
        motion.Advance(0);
        Assert.That(motion.Running, Is.False);
        Assert.That(sprite.Modulate, Is.EqualTo(Color.Blue));
        Assert.That(sprite.Scale, Is.EqualTo(new Vector2(2)));
    }

    [Test]
    public void VisualOnlyAnimationDoesNotChangeButtonInputOrLayout()
    {
        _window = new TestWindow();
        var checkbox = new TestCheckbox { MuteSounds = true, Mode = BaseButton.ActionMode.Press, SetSize = new Vector2(64) };
        _window.AddChild(checkbox);
        _window.Open();
        var filter = checkbox.MouseFilter;
        var size = checkbox.SetSize;
        var motion = new UiMotionVisual(checkbox.TextureRect, _configuration, S.Slots);
        motion.Start(100);
        checkbox.Click();
        Assert.That(checkbox.Pressed, Is.True);
        Assert.That(checkbox.MouseFilter, Is.EqualTo(filter));
        Assert.That(checkbox.SetSize, Is.EqualTo(size));
        motion.Reset();
    }

    [Test]
    public void ReversingAVisualKeepsItsCurrentOpacity()
    {
        _window = new TestWindow();
        var visual = new Control();
        _window.AddChild(visual);
        _window.Open();
        var motion = new UiMotionVisual(visual, _configuration, S.Slots);
        motion.Start(100);
        motion.Advance(0.02f);
        var current = visual.Modulate;
        motion.Start(100, false);
        Assert.That(visual.Modulate, Is.EqualTo(current));
        motion.Advance(0.2f);
        Assert.That(visual.Modulate.A, Is.Zero);
        motion.Reset();
        Assert.That(visual.Modulate, Is.EqualTo(Color.White));
    }

    [Test]
    public void NewDurationsAndScalesRejectNonFiniteValues()
    {
        var milliseconds = new[] { S.PopupsMs, S.DropdownEnterMs, S.DropdownItemMs,
            S.SlotEnterMs, S.SlotExitMs, S.ActionsEnterMs,
            S.AlertsEnterMs, S.ChatMs, S.ProgressMs,
            S.RadialEnterMs, S.ListsEnterMs };
        foreach (var setting in milliseconds)
        {
            _configuration.SetCVar(setting, float.NaN);
            Assert.That(S.Read(_configuration, setting, 0, 1000), Is.EqualTo(setting.DefaultValue), setting.Name);
            _configuration.SetCVar(setting, -1f);
            Assert.That(S.Read(_configuration, setting, 0, 1000), Is.Zero, setting.Name);
        }
        _configuration.SetCVar(S.RadialHoverScale, -1f);
        _configuration.SetCVar(S.ButtonHoverIntensity, float.NaN);
        _configuration.SetCVar(S.ButtonPressIntensity, -1f);
        _configuration.SetCVar(S.DropdownTravel, float.PositiveInfinity);
        _configuration.SetCVar(S.AlertsTravel, float.NaN);
        Assert.That(S.Read(_configuration, S.RadialHoverScale, 0, 0.15f), Is.Zero);
        Assert.That(S.Read(_configuration, S.ButtonHoverIntensity, 0, 0.2f), Is.EqualTo(S.ButtonHoverIntensity.DefaultValue));
        Assert.That(S.Read(_configuration, S.ButtonPressIntensity, 0, 0.2f), Is.Zero);
        Assert.That(S.Read(_configuration, S.DropdownTravel, 0, 24), Is.EqualTo(S.DropdownTravel.DefaultValue));
        Assert.That(S.Read(_configuration, S.AlertsTravel, 0, 240), Is.EqualTo(S.AlertsTravel.DefaultValue));
    }

    [Test]
    public void ListRowsOwnTheirFadeAndCanBeDisabledSeparately()
    {
        _window = new TestWindow();
        var row = new UiMotionListRow();
        var button = new Button();
        row.AddChild(button);
        _window.AddChild(row);
        _window.Open();
        _controller.FrameUpdate(new FrameEventArgs(0.02f));
        Assert.That(row.Modulate.A, Is.LessThan(1));
        Assert.That(button.Modulate, Is.EqualTo(Color.White));
        _configuration.SetCVar(S.Lists, false);
        _controller.FrameUpdate(new FrameEventArgs(0));
        Assert.That(row.Modulate, Is.EqualTo(Color.White));
    }

    [TestCase(float.NaN, 150f)]
    [TestCase(float.PositiveInfinity, 150f)]
    [TestCase(-50f, 0f)]
    [TestCase(5000f, 1000f)]
    public void PopupDurationIsNormalized(float input, float expected)
    {
        _configuration.SetCVar(S.PopupsMs, input);
        Assert.That(S.Read(_configuration, S.PopupsMs, 0, 1000), Is.EqualTo(expected));
    }

    [Test]
    public void ProgressIsBoundedMonotoneAndCompletesImmediatelyAtEndpoints()
    {
        var progress = new UiMotionProgressValue();
        Assert.That(progress.Advance(0.2f, 0, 0.12f, true), Is.EqualTo(0.2f));
        var previous = 0.2f;
        for (var i = 0; i < 15; i++)
        {
            var displayed = progress.Advance(0.8f, 0.01f, 0.12f, true);
            Assert.That(displayed, Is.InRange(previous, 0.800001f));
            previous = displayed;
        }
        Assert.That(previous, Is.EqualTo(0.8f).Within(0.00001));
        Assert.That(progress.Advance(1, 0, 0.12f, true), Is.EqualTo(1));
        Assert.That(progress.Advance(0, 0, 0.12f, true), Is.Zero);
        Assert.That(progress.Advance(0.3f, 0, 0.12f, false), Is.EqualTo(0.3f));
        Assert.That(progress.Advance(0.3f, 0, 0.12f, true), Is.EqualTo(0.3f));
        Assert.That(progress.Advance(float.NaN, 0, 0.12f, true), Is.Zero);
    }

    [Test]
    public void MaterialCurvesKeepEndpointsAndExpressiveSpatialMotionOvershoots()
    {
        Assert.That(UiMotionEasing.Spatial(0, true, UiMotionEasing.FastSpatialSeconds), Is.Zero);
        Assert.That(UiMotionEasing.Spatial(1, true, UiMotionEasing.FastSpatialSeconds), Is.EqualTo(1));
        Assert.That(UiMotionEasing.Effects(0, UiMotionEasing.FastEffectsSeconds), Is.Zero);
        Assert.That(UiMotionEasing.Effects(1, UiMotionEasing.FastEffectsSeconds), Is.EqualTo(1));

        var previous = 0f;
        var overshot = false;
        for (var i = 1; i <= 20; i++)
        {
            var progress = i / 20f;
            var effect = UiMotionEasing.Effects(progress, UiMotionEasing.DefaultEffectsSeconds);
            Assert.That(effect, Is.InRange(previous, 1f));
            previous = effect;
            overshot |= UiMotionEasing.Spatial(progress, true, UiMotionEasing.FastSpatialSeconds) > 1;
        }
        Assert.That(overshot, Is.True);
    }

    [Test]
    public void InteractiveSpringRetargetsWithoutJumpAndSettles()
    {
        var spring = new UiMotionSpring();
        for (var i = 0; i < 6; i++)
            spring.Advance(1, 1 / 60f);
        var beforeRetarget = spring.Value;
        Assert.That(beforeRetarget, Is.GreaterThan(0));

        var afterRetarget = spring.Advance(0, 0);
        Assert.That(afterRetarget, Is.EqualTo(beforeRetarget));
        for (var i = 0; i < 120; i++)
            spring.Advance(0, 1 / 60f);
        Assert.That(spring.Value, Is.EqualTo(0).Within(0.001f));
        Assert.That(spring.Velocity, Is.EqualTo(0).Within(0.01f));
    }
}
// Arcane-End
