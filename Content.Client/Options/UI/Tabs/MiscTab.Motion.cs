// Arcane-Start
using System.Globalization;
using Content.Client.UserInterface.Motion;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using S = Content.Client.UserInterface.Motion.UiMotionSettings;

namespace Content.Client.Options.UI.Tabs;

public sealed partial class MiscTab
{
    [Dependency] private IConfigurationManager _motionConfiguration = default!;

    private readonly List<BaseOption> _motionOptions = [];
    private readonly Dictionary<CVarDef<bool>, CheckBox> _motionChecks = [];
    private readonly Dictionary<CVarDef<float>, MotionFloatOption> _motionSliders = [];
    private UiMotionPreview? _motionPreview;

    private sealed record MotionGroup(BoxContainer Basic, BoxContainer Details, List<BaseOption> Options);

    private void InitializeMotionOptions()
    {
        var general = AddMotionGroup("general");
        MotionCheck(general, S.Enabled, "enabled");
        MotionSlider(general, S.Speed, "speed", S.MinSpeed, S.MaxSpeed, "multiplier");
        var profiles = new BoxContainer();
        general.Basic.AddChild(profiles);
        foreach (var profile in new[] { "subtle", "expressive", "off" })
        {
            var button = new Button { Text = Loc.GetString("ui-motion-profile-" + profile) };
            button.OnPressed += _ => ApplyMotionProfile(profile);
            profiles.AddChild(button);
        }
        var preview = new Button { Text = Loc.GetString("ui-motion-preview") };
        preview.OnPressed += _ =>
        {
            _motionPreview?.Close();
            _motionPreview = new UiMotionPreview();
            _motionPreview.OpenCentered();
        };
        general.Basic.AddChild(preview);
        OnVisibilityChanged += _ =>
        {
            if (!VisibleInTree)
                _motionPreview?.Close();
        };

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

        var buttons = AddMotionGroup("buttons");
        MotionCheck(buttons, S.Buttons, "enabled");
        var order = new OptionDropDown { Title = Loc.GetString("ui-motion-order") };
        buttons.Basic.AddChild(order);
        Track(buttons, Control.AddOptionDropDown(S.ButtonOrder, order,
            new List<OptionDropDownCVar<int>.ValueOption>
            {
                new(0, Loc.GetString("ui-motion-order-together")),
                new(1, Loc.GetString("ui-motion-order-sequential")),
                new(2, Loc.GetString("ui-motion-order-jitter")),
                new(3, Loc.GetString("ui-motion-order-random"))
            }));
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

        var speech = AddMotionGroup("speech");
        MotionCheck(speech, S.SpeechTyping, "typing");
        MotionCheck(speech, S.SpeechPunctuation, "punctuation");
        MotionSlider(speech, S.SpeechCharactersPerSecond, "characters", 10, 120, "characters");
        MotionSlider(speech, S.SpeechMaxSeconds, "max-reveal", 0.1f, 2.5f, "seconds");
        MotionSlider(speech, S.SpeechPunctuationMs, "pause", 0, 500, "ms");
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
        Track(group, Control.AddOptionCheckBox(setting, checkbox));
    }

    private void MotionSlider(MotionGroup group, CVarDef<float> setting, string label, float min, float max, string unit)
    {
        var slider = new OptionSlider { Title = Loc.GetString("ui-motion-" + label) };
        group.Details.AddChild(slider);
        var option = Control.AddOption(new MotionFloatOption(Control, _motionConfiguration, setting, slider, min, max, unit));
        _motionSliders.Add(setting, option);
        Track(group, option);
    }

    private void ApplyMotionProfile(string profile)
    {
        if (profile == "off")
            _motionChecks[S.Enabled].Pressed = false;
        else
        {
            foreach (var option in _motionOptions)
                option.ResetToDefault();
            if (profile == "subtle")
            {
                _motionChecks[S.MenuSpring].Pressed = false;
                _motionChecks[S.BubbleMovement].Pressed = false;
                _motionSliders[S.MenuTravel].Set(0);
                _motionSliders[S.ButtonJitterMs].Set(0);
                _motionSliders[S.CheckboxIntensity].Set(0.15f);
                _motionSliders[S.WindowEnterMs].Set(150);
            }
        }
        Control.ValueChanged();
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

        private void UpdateLabel()
        {
            var format = _unit is "seconds" or "multiplier" or "ratio" ? "0.00" : "0";
            _slider.ValueLabel.Text = Loc.GetString("ui-motion-value-" + _unit,
                ("value", Value.ToString(format, CultureInfo.CurrentCulture)));
        }
    }
}
// Arcane-End
