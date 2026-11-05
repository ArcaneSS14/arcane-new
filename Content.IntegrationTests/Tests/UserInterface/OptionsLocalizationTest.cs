// Arcane-Start
using System.Globalization;
using System.Linq;
using Content.Client.Localizations;
using Content.Client.Gameplay;
using Content.Client.Options.UI;
using Content.Client.Options.UI.Tabs;
using Content.Client.UserInterface.Systems.EscapeMenu;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.UserInterface;

[TestFixture]
public sealed class OptionsLocalizationTest : GameTest
{
    private static PoolSettings ReconnectSettings => new() { Connected = true, DummyTicker = false };

    [Test]
    public async Task RefreshPreservesPendingValues()
    {
        await Client.WaitAssertion(() =>
        {
            var ui = Client.ResolveDependency<IUserInterfaceManager>();
            var loc = Client.ResolveDependency<ILocalizationManager>();
            var culture = Client.CfgMan.GetCVar(CVars.LocCultureName);
            var controller = ui.GetUIController<OptionsUIController>();
            controller.OpenWindow();
            var window = ui.WindowRoot.Children.OfType<OptionsMenu>().Single();
            var graphics = window.FindControl<GraphicsTab>("GraphicsTab");
            var audio = window.FindControl<AudioTab>("AudioTab");
            var network = window.FindControl<NetworkTab>("NetworkTab");
            var slider = graphics.FindControl<OptionSlider>("SharpnessSlider");
            var filter = graphics.FindControl<OptionDropDown>("DropDownFilterMode");
            var hearSelf = audio.FindControl<CheckBox>("HearSelfCheckBox");
            var prediction = network.FindControl<CheckBox>("NetPredictCheckbox");
            try
            {
                slider.Slider.Value = slider.Slider.Value == 0 ? 10 : 0;
                filter.Button.SelectId(1);
                hearSelf.Pressed = !hearSelf.Pressed;
                prediction.Pressed = !prediction.Pressed;
                var value = slider.Slider.Value;
                var hear = hearSelf.Pressed;
                var predict = prediction.Pressed;
                var apply = graphics.Control.FindControl<Button>("ApplyButton").Disabled;
                var selectedTab = window.Tabs.CurrentTab;

                Client.CfgMan.SetCVar(CVars.LocCultureName, OtherCulture().Name);

                Assert.Multiple(() =>
                {
                    Assert.That(slider.Slider.Value, Is.EqualTo(value));
                    Assert.That(filter.Button.SelectedMetadata, Is.EqualTo("bilinear"));
                    Assert.That(hearSelf.Pressed, Is.EqualTo(hear));
                    Assert.That(prediction.Pressed, Is.EqualTo(predict));
                    Assert.That(graphics.Control.FindControl<Button>("ApplyButton").Disabled, Is.EqualTo(apply));
                    Assert.That(window.Tabs.CurrentTab, Is.EqualTo(selectedTab));
                    Assert.That(slider.ValueLabel.Text, Is.EqualTo(loc.GetString("ui-options-value-percent", ("value", value / 10f))));
                    Assert.That(SelectedLabel(filter), Is.EqualTo(loc.GetString("ui-options-filter-bilinear")));
                });
                AssertLocalized(window, loc);
            }
            finally
            {
                Client.CfgMan.SetCVar(CVars.LocCultureName, culture);
                window.UpdateTabs();
                window.Close();
            }
        });
    }

    [Test]
    [PairConfig(nameof(ReconnectSettings))]
    public async Task RefreshAfterReconnect()
    {
        var ui = Client.ResolveDependency<IUserInterfaceManager>();
        OptionsMenu window = null!;
        await Client.WaitAssertion(() =>
        {
            Assert.That(Client.ResolveDependency<IStateManager>().CurrentState, Is.TypeOf<GameplayState>());
            ui.GetUIController<OptionsUIController>().OpenWindow();
            window = ui.WindowRoot.Children.OfType<OptionsMenu>().Single();
        });

        await Pair.Disconnect();
        await Client.WaitAssertion(() => Assert.That(ui.TryGetFirstWindow<OptionsMenu>(out _), Is.False));
        await Pair.Connect();
        await Client.WaitAssertion(() =>
        {
            var culture = Client.CfgMan.GetCVar(CVars.LocCultureName);
            try
            {
                ui.GetUIController<OptionsUIController>().OpenWindow();
                Assert.That(window.IsOpen, Is.True, "The controller must reuse its existing window.");
                Client.CfgMan.SetCVar(CVars.LocCultureName, OtherCulture().Name);
                AssertLocalized(window, Client.ResolveDependency<ILocalizationManager>());
            }
            finally
            {
                Client.CfgMan.SetCVar(CVars.LocCultureName, culture);
                window.Close();
            }
        });
    }

    [Test]
    public async Task CultureCacheChangesOnlyForActualSwitch()
    {
        await Client.WaitAssertion(() =>
        {
            var loc = Client.ResolveDependency<ILocalizationManager>();
            var manager = Client.ResolveDependency<DynamicLocalizationManager>();
            var prototypes = Client.ResolveDependency<IPrototypeManager>();
            var prototype = prototypes.EnumeratePrototypes<EntityPrototype>().First(p => loc.HasString($"ent-{p.ID}"));
            var culture = Client.CfgMan.GetCVar(CVars.LocCultureName);
            var notifications = 0;
            void Changed(CultureInfo previous, CultureInfo current) => notifications++;
            manager.CultureChanged += Changed;
            try
            {
                var cached = loc.GetEntityData(prototype.ID);
                Client.CfgMan.SetCVar(CVars.LocCultureName, loc.DefaultCulture!.Name.ToUpperInvariant());
                Assert.That(loc.GetEntityData(prototype.ID), Is.SameAs(cached));
                Assert.That(notifications, Is.Zero);

                Client.CfgMan.SetCVar(CVars.LocCultureName, OtherCulture().Name);
                var refreshed = loc.GetEntityData(prototype.ID);
                Assert.That(refreshed, Is.Not.SameAs(cached));
                Assert.That(refreshed.Name, Is.EqualTo(loc.GetString($"ent-{prototype.ID}")));
                Assert.That(notifications, Is.EqualTo(1));
            }
            finally
            {
                manager.CultureChanged -= Changed;
                Client.CfgMan.SetCVar(CVars.LocCultureName, culture);
            }
        });
    }

    private CultureInfo OtherCulture()
    {
        var manager = Client.ResolveDependency<DynamicLocalizationManager>();
        var current = Client.ResolveDependency<ILocalizationManager>().DefaultCulture!;
        return manager.GetAvailableCultures().First(culture => !culture.Equals(current));
    }

    private static string? SelectedLabel(OptionDropDown dropDown)
        => dropDown.Button.Children.Single().Children.OfType<Label>().Single().Text;

    private static void AssertLocalized(OptionsMenu window, ILocalizationManager loc)
    {
        Assert.Multiple(() =>
        {
            Assert.That(window.Title, Is.EqualTo(loc.GetString("ui-options-title")));
            Assert.That(window.FindControl<MiscTab>("MiscTab").FindControl<OptionDropDown>("DropDownHudTheme").Title, Is.EqualTo(loc.GetString("ui-options-hud-theme")));
            Assert.That(window.FindControl<GraphicsTab>("GraphicsTab").FindControl<CheckBox>("VSyncCheckBox").Text, Is.EqualTo(loc.GetString("ui-options-vsync")));
            Assert.That(window.FindControl<KeyRebindTab>("KeyRebindTab").FindControl<Button>("ResetAllButton").Text, Is.EqualTo(loc.GetString("ui-options-binds-reset-all")));
            Assert.That(window.FindControl<AudioTab>("AudioTab").FindControl<CheckBox>("HearSelfCheckBox").Text, Is.EqualTo(loc.GetString("ui-options-hear-self")));
            Assert.That(window.FindControl<AccessibilityTab>("AccessibilityTab").FindControl<CheckBox>("ReducedMotionCheckBox").ToolTip, Is.EqualTo(loc.GetString("ui-options-reduced-motion-tooltip")));
            Assert.That(window.FindControl<AdminOptionsTab>("AdminOptionsTab").FindControl<OptionDropDown>("DropDownOverlayAntagFormat").Title, Is.EqualTo(loc.GetString("ui-options-admin-overlay-antag-format")));
            Assert.That(window.FindControl<NetworkTab>("NetworkTab").FindControl<CheckBox>("NetPredictCheckbox").Text, Is.EqualTo(loc.GetString("ui-options-net-predict")));
            Assert.That(window.FindControl<CustomizationTab>("CustomizationTab").FindControl<OptionDropDown>("RunechatFontDropDown").Title, Is.EqualTo(loc.GetString("ui-options-runechat-font")));
        });
    }
}
// Arcane-End
