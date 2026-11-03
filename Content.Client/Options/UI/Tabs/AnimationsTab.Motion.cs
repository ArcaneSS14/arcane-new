// Arcane-Start
using System.Globalization;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using S = Content.Client.UserInterface.Motion.UiMotionSettings;
using System.Linq;

namespace Content.Client.Options.UI.Tabs;

public sealed partial class AnimationsTab
{
    [Dependency] private IConfigurationManager _motionConfiguration = default!;

    private readonly List<BaseOption> _motionOptions = [];
    private readonly Dictionary<CVarDef<bool>, CheckBox> _motionChecks = [];
    private readonly Dictionary<CVarDef<float>, MotionFloatOption> _motionSliders = [];
    private bool _profileReady;
    private bool _updatingProfile;

    private sealed record MotionGroup(BoxContainer Basic, BoxContainer Details, List<BaseOption> Options);

    private enum MotionPreset : byte
    {
        Expressive,
        Subtle,
        Reduced,
        Off,
        Custom
    }

    private void InitializeMotionOptions()
    {
        InitializeProfileSelector();
        var general = AddMotionGroup("general");
        MotionCheck(general, S.Enabled, "enabled");
        MotionCheck(general, S.Reduce, "reduce");
        MotionSlider(general, S.Speed, "speed", S.MinSpeed, S.MaxSpeed, "multiplier");

        var menus = AddMotionGroup("menus");
        MotionCheck(menus, S.MenuOpening, "opening");
        MotionCheck(menus, S.AnimateClosing, "closing");
        MotionCheck(menus, S.MenuSequence, "sequence");
        MotionCheck(menus, S.MenuFade, "fade");
        MotionCheck(menus, S.MenuSpring, "spring");
        MotionSlider(menus, S.MenuEnterMs, "enter", 0, 1000, "ms");
        MotionSlider(menus, S.MenuExitMs, "exit", 0, 1000, "ms");
        MotionSlider(menus, S.MenuTravel, "travel", 0, 24, "pixels");
        MotionSlider(menus, S.MenuSequenceMs, "sequence-duration", 0, 500, "ms");

        var windows = AddMotionGroup("windows");
        MotionCheck(windows, S.Windows, "opening");
        MotionSlider(windows, S.WindowEnterMs, "enter", 0, 1000, "ms");
        MotionSlider(windows, S.WindowTravel, "travel", 0, 24, "pixels");

        var buttons = AddMotionGroup("buttons");
        MotionCheck(buttons, S.Buttons, "enabled");
        var order = new OptionDropDown { Title = Loc.GetString("ui-motion-order") };
        buttons.Basic.AddChild(order);
        var orderOption = Control.AddOptionDropDown(S.ButtonOrder, order,
            new List<OptionDropDownCVar<int>.ValueOption>
            {
                new(0, Loc.GetString("ui-motion-order-together")),
                new(1, Loc.GetString("ui-motion-order-sequential")),
                new(2, Loc.GetString("ui-motion-order-jitter")),
                new(3, Loc.GetString("ui-motion-order-random"))
            });
        orderOption.ImmediateValueChanged += _ => RefreshProfileSelection();
        Track(buttons, orderOption);
        MotionSlider(buttons, S.ButtonEnterMs, "enter", 0, 1000, "ms");
        MotionSlider(buttons, S.ButtonIntervalMs, "interval", 0, 150, "ms");
        MotionSlider(buttons, S.ButtonJitterMs, "jitter", 0, 150, "ms");
        MotionSlider(buttons, S.ButtonMaxDelayMs, "max-delay", 0, 1000, "ms");

        var checkboxes = AddMotionGroup("checkboxes");
        MotionCheck(checkboxes, S.Checkboxes, "enabled");
        MotionSlider(checkboxes, S.CheckboxMs, "duration", 0, 1000, "ms");
        MotionSlider(checkboxes, S.CheckboxIntensity, "intensity", 0, 0.8f, "ratio");

        var bubbles = AddMotionGroup("bubbles");
        MotionCheck(bubbles, S.BubbleOpening, "opening");
        MotionCheck(bubbles, S.BubbleClosing, "closing");
        MotionCheck(bubbles, S.BubbleMovement, "movement");
        MotionSlider(bubbles, S.BubbleEnterMs, "enter", 0, 1000, "ms");
        MotionSlider(bubbles, S.BubbleExitMs, "exit", 0, 1000, "ms");

        var popups = AddMotionGroup("popups");
        MotionCheck(popups, S.Popups, "enabled");
        MotionSlider(popups, S.PopupsMs, "duration", 0, 1000, "ms");

        var slots = AddMotionGroup("slots");
        MotionCheck(slots, S.Slots, "enabled");
        MotionSlider(slots, S.SlotEnterMs, "duration", 0, 1000, "ms");
        MotionSlider(slots, S.SlotExitMs, "exit", 0, 1000, "ms");

        var actions = AddMotionGroup("actions");
        MotionCheck(actions, S.Actions, "enabled");
        MotionSlider(actions, S.ActionsEnterMs, "duration", 0, 1000, "ms");

        var alerts = AddMotionGroup("alerts");
        MotionCheck(alerts, S.Alerts, "enabled");
        MotionSlider(alerts, S.AlertsEnterMs, "duration", 0, 1000, "ms");
        MotionSlider(alerts, S.AlertsExitMs, "exit", 0, 1000, "ms");

        var chat = AddMotionGroup("chat");
        MotionCheck(chat, S.Chat, "enabled");
        MotionSlider(chat, S.ChatMs, "duration", 0, 1000, "ms");

        var notifications = AddMotionGroup("notifications");
        MotionCheck(notifications, S.Notifications, "enabled");
        MotionSlider(notifications, S.NotificationsMs, "duration", 0, 1000, "ms");

        var progress = AddMotionGroup("progress");
        MotionCheck(progress, S.Progress, "enabled");
        MotionSlider(progress, S.ProgressMs, "duration", 0, 1000, "ms");

        var radial = AddMotionGroup("radial");
        MotionCheck(radial, S.Radial, "enabled");
        MotionSlider(radial, S.RadialEnterMs, "duration", 0, 1000, "ms");
        MotionSlider(radial, S.RadialExitMs, "exit", 0, 1000, "ms");

        var lists = AddMotionGroup("lists");
        MotionCheck(lists, S.Lists, "enabled");
        MotionSlider(lists, S.ListsEnterMs, "duration", 0, 1000, "ms");

        var tabs = AddMotionGroup("tabs");
        MotionCheck(tabs, S.Tabs, "enabled");
        MotionSlider(tabs, S.TabsEnterMs, "duration", 0, 1000, "ms");

        var votes = AddMotionGroup("votes");
        MotionCheck(votes, S.Votes, "enabled");
        MotionSlider(votes, S.VotesEnterMs, "enter", 0, 1000, "ms");
        MotionSlider(votes, S.VotesTravel, "travel", 0, 240, "pixels");

        MotionCheck(slots, S.SlotHighlight, "highlight");
        MotionSlider(slots, S.SlotScale, "scale", 0, 0.25f, "ratio");
        MotionCheck(actions, S.ActionsCooldownPulse, "cooldown-pulse");
        MotionCheck(alerts, S.AlertsSeverityPulse, "severity-pulse");
        MotionCheck(radial, S.RadialHover, "hover");
        MotionSlider(radial, S.RadialHoverScale, "scale", 0, 0.15f, "ratio");
        MotionSlider(lists, S.ListsMaxStagger, "max-stagger", 0, 32, "items");

        var speech = AddMotionGroup("speech");
        MotionCheck(speech, S.SpeechTyping, "typing");
        MotionCheck(speech, S.SpeechPunctuation, "punctuation");
        MotionSlider(speech, S.SpeechCharactersPerSecond, "characters", 10, 120, "characters");
        MotionSlider(speech, S.SpeechMaxSeconds, "max-reveal", 0.1f, 2.5f, "seconds");
        MotionSlider(speech, S.SpeechPunctuationMs, "pause", 0, 500, "ms");
    }

    private void InitializeProfileSelector()
    {
        foreach (var preset in Enum.GetValues<MotionPreset>())
            MotionProfile.Button.AddItem(Loc.GetString("ui-motion-profile-" + preset.ToString().ToLowerInvariant()), (int) preset);
        MotionProfile.Button.SetItemDisabled(MotionProfile.Button.GetIdx((int) MotionPreset.Custom), true);
        MotionProfile.Button.OnItemSelected += args =>
        {
            var preset = (MotionPreset) args.Id;
            if (preset == MotionPreset.Custom)
                return;
            ApplyMotionProfile(preset);
        };
    }

    private MotionGroup AddMotionGroup(string name)
    {
        MotionOptions.AddChild(new Label { Text = Loc.GetString("ui-motion-group-" + name), StyleClasses = { "LabelKeyText" } });
        var basic = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
        var details = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, Visible = false };
        var row = new BoxContainer();
        var expand = new Button { Text = Loc.GetString("ui-motion-details"), ToggleMode = true };
        expand.OnToggled += args => details.Visible = args.Pressed;
        row.AddChild(expand);
        var options = new List<BaseOption>();
        var reset = new Button { Text = Loc.GetString("ui-motion-reset-group") };
        reset.OnPressed += _ =>
        {
            foreach (var option in options)
                option.ResetToDefault();
            Control.ValueChanged();
            RefreshProfileSelection();
        };
        row.AddChild(reset);
        MotionOptions.AddChild(basic);
        MotionOptions.AddChild(row);
        MotionOptions.AddChild(details);
        return new MotionGroup(basic, details, options);
    }

    private void Track(MotionGroup group, BaseOption option)
    {
        group.Options.Add(option);
        _motionOptions.Add(option);
    }

    private void MotionCheck(MotionGroup group, CVarDef<bool> setting, string label)
    {
        var checkbox = new CheckBox { Text = Loc.GetString("ui-motion-" + label) };
        group.Basic.AddChild(checkbox);
        _motionChecks.Add(setting, checkbox);
        var option = Control.AddOptionCheckBox(setting, checkbox);
        option.ImmediateValueChanged += _ => RefreshProfileSelection();
        Track(group, option);
    }

    private void MotionSlider(MotionGroup group, CVarDef<float> setting, string label, float min, float max, string unit)
    {
        var slider = new OptionSlider { Title = Loc.GetString("ui-motion-" + label) };
        group.Details.AddChild(slider);
        var option = Control.AddOption(new MotionFloatOption(Control, _motionConfiguration, setting, slider, min, max, unit));
        option.ImmediateValueChanged += _ => RefreshProfileSelection();
        _motionSliders.Add(setting, option);
        Track(group, option);
    }

    private void ApplyMotionProfile(MotionPreset profile)
    {
        _updatingProfile = true;
        try
        {
            if (profile == MotionPreset.Off)
                _motionChecks[S.Enabled].Pressed = false;
            else
            {
                foreach (var option in _motionOptions)
                    option.ResetToDefault();
                _motionChecks[S.Reduce].Pressed = profile == MotionPreset.Reduced;
                if (profile == MotionPreset.Subtle)
                {
                    _motionChecks[S.MenuSpring].Pressed = false;
                    _motionChecks[S.BubbleMovement].Pressed = false;
                    _motionSliders[S.MenuTravel].Set(0);
                    _motionSliders[S.ButtonJitterMs].Set(0);
                    _motionSliders[S.CheckboxIntensity].Set(0.15f);
                    _motionSliders[S.WindowEnterMs].Set(150);
                    _motionSliders[S.WindowTravel].Set(0);
                    _motionChecks[S.ActionsCooldownPulse].Pressed = false;
                    _motionChecks[S.AlertsSeverityPulse].Pressed = false;
                    _motionChecks[S.RadialHover].Pressed = false;
                    _motionSliders[S.SlotScale].Set(0.03f);
                    _motionSliders[S.PopupsMs].Set(70);
                    _motionSliders[S.AlertsEnterMs].Set(100);
                    _motionSliders[S.ListsEnterMs].Set(100);
                }
            }
            Control.ValueChanged();
        }
        finally
        {
            _updatingProfile = false;
        }
        SelectProfile(profile);
    }

    private void RefreshProfileSelection()
    {
        if (!_profileReady || _updatingProfile)
            return;
        SelectProfile(DetectProfile());
    }

    private MotionPreset DetectProfile()
    {
        if (!_motionChecks[S.Enabled].Pressed)
            return MotionPreset.Off;

        var modified = _motionOptions.Count(option => option.IsModifiedFromDefault());
        if (modified == 0)
            return MotionPreset.Expressive;
        if (modified == 1 && _motionChecks[S.Reduce].Pressed)
            return MotionPreset.Reduced;
        return modified == 14 && IsSubtleProfile() ? MotionPreset.Subtle : MotionPreset.Custom;
    }

    private bool IsSubtleProfile()
    {
        return !_motionChecks[S.MenuSpring].Pressed
            && !_motionChecks[S.BubbleMovement].Pressed
            && !_motionChecks[S.ActionsCooldownPulse].Pressed
            && !_motionChecks[S.AlertsSeverityPulse].Pressed
            && !_motionChecks[S.RadialHover].Pressed
            && Matches(S.MenuTravel, 0)
            && Matches(S.ButtonJitterMs, 0)
            && Matches(S.CheckboxIntensity, 0.15f)
            && Matches(S.WindowEnterMs, 150)
            && Matches(S.WindowTravel, 0)
            && Matches(S.SlotScale, 0.03f)
            && Matches(S.PopupsMs, 70)
            && Matches(S.AlertsEnterMs, 100)
            && Matches(S.ListsEnterMs, 100);
    }

    private bool Matches(CVarDef<float> setting, float expected)
    {
        return MathF.Abs(_motionSliders[setting].Current - expected) < 0.001f;
    }

    private void SelectProfile(MotionPreset profile)
    {
        MotionProfile.Button.SelectId((int) profile);
        MotionProfileDescription.Text = Loc.GetString("ui-motion-profile-description-" + profile.ToString().ToLowerInvariant());
    }

    private sealed class MotionFloatOption : BaseOptionCVar<float>
    {
        private readonly OptionSlider _slider;
        private readonly CVarDef<float> _setting;
        private readonly float _min;
        private readonly float _max;
        private readonly string _unit;

        protected override float Value
        {
            get => S.Normalize(_slider.Slider.Value, _min, _max, _setting.DefaultValue);
            set
            {
                _slider.Slider.Value = S.Normalize(value, _min, _max, _setting.DefaultValue);
                UpdateLabel();
            }
        }

        public MotionFloatOption(OptionsTabControlRow controller, IConfigurationManager configuration, CVarDef<float> setting,
            OptionSlider slider, float min, float max, string unit) : base(controller, configuration, setting)
        {
            _slider = slider;
            _setting = setting;
            _min = min;
            _max = max;
            _unit = unit;
            slider.Slider.MinValue = min;
            slider.Slider.MaxValue = max;
            slider.Slider.OnValueChanged += _ =>
            {
                ValueChanged();
                UpdateLabel();
            };
        }

        public void Set(float value) => Value = value;
        public float Current => Value;

        private void UpdateLabel()
        {
            var format = _unit is "seconds" or "multiplier" or "ratio" ? "0.00" : "0";
            _slider.ValueLabel.Text = Loc.GetString("ui-motion-value-" + _unit,
                ("value", Value.ToString(format, CultureInfo.CurrentCulture)));
        }
    }
}
// Arcane-End
