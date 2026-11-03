// Arcane-Start
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Motion;

/// <summary>
/// Observes the window tree through public events. Only active transitions receive per-frame work.
/// Does not reparent controls, defer close handlers, or change their hit areas.
/// </summary>
public sealed partial class UiMotionUIController : UIController
{
    [Dependency] private IConfigurationManager _configuration = default!;

    private readonly HashSet<Control> _observed = [];
    private readonly List<Control> _pending = [];
    private readonly Dictionary<Control, Fade> _active = [];
    private readonly List<Control> _completed = [];

    private sealed class Fade
    {
        public required UiMotionTransition Transition;
        public required CVarDef<bool> Enabled;
        public Color Original;
        public Color Applied;
        public float Delay;
    }

    public override void Initialize()
    {
        base.Initialize();
        Attach(UIManager.WindowRoot);
    }

    private void Attach(Control control)
    {
        if (!_observed.Add(control))
            return;

        control.OnChildAdded += Attach;
        control.OnChildRemoved += Detach;
        control.OnVisibilityChanged += VisibilityChanged;
        if (control is CheckBox checkbox)
            checkbox.OnToggled += CheckboxToggled;
        Queue(control);
        foreach (var child in control.Children)
            Attach(child);
    }

    private void Detach(Control control)
    {
        if (!_observed.Remove(control))
            return;

        control.OnChildAdded -= Attach;
        control.OnChildRemoved -= Detach;
        control.OnVisibilityChanged -= VisibilityChanged;
        if (control is CheckBox checkbox)
            checkbox.OnToggled -= CheckboxToggled;
        _pending.Remove(control);
        Cancel(control);
        foreach (var child in control.Children)
            Detach(child);
    }

    private void VisibilityChanged(Control control)
    {
        if (control.VisibleInTree)
            Queue(control);
        else
            Cancel(control);
    }

    private void Queue(Control control)
    {
        if (control.VisibleInTree && control is BaseWindow or BaseButton && !_pending.Contains(control))
            _pending.Add(control);
    }

    private void CheckboxToggled(BaseButton.ButtonToggledEventArgs args)
    {
        if (args.Button is not CheckBox checkbox)
            return;
        Start(checkbox.TextureRect, UiMotionSettings.Checkboxes,
            Read(UiMotionSettings.CheckboxMs, 0, 1000), 0,
            1 - Read(UiMotionSettings.CheckboxIntensity, 0, 0.8f));
    }

    private float Read(CVarDef<float> setting, float min, float max) => UiMotionSettings.Read(_configuration, setting, min, max);

    private void Start(Control control, CVarDef<bool> enabled, float milliseconds, float delay, float from = 0)
    {
        // Restart from the currently displayed opacity when toggling again.
        if (_active.TryGetValue(control, out var previous))
            from = previous.Transition.Opacity;
        Cancel(control);
        if (!_configuration.GetCVar(UiMotionSettings.Enabled) || !_configuration.GetCVar(enabled) || milliseconds <= 0)
            return;

        var transition = new UiMotionTransition(_configuration);
        transition.Start(new UiMotionProfile(TimeSpan.FromMilliseconds(milliseconds), TimeSpan.Zero, 0, false), true, from, 0);
        var fade = new Fade
        {
            Transition = transition,
            Enabled = enabled,
            Original = control.Modulate,
            Applied = control.Modulate.WithAlpha(control.Modulate.A * from),
            Delay = delay
        };
        _active.Add(control, fade);
        control.Modulate = fade.Applied;
    }

    private void Cancel(Control control)
    {
        if (_active.Remove(control, out var fade) && control.Modulate == fade.Applied)
            control.Modulate = fade.Original;
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        var index = 0;
        foreach (var control in _pending)
        {
            if (!control.IsInsideTree || !control.VisibleInTree)
                continue;
            if (control is BaseWindow)
                Start(control, UiMotionSettings.Windows, Read(UiMotionSettings.WindowEnterMs, 0, 1000), 0);
            else
            {
                var delay = UiMotionSequence.Delay(index++, _configuration.GetCVar(UiMotionSettings.ButtonOrder),
                    Read(UiMotionSettings.ButtonIntervalMs, 0, 150), Read(UiMotionSettings.ButtonJitterMs, 0, 150),
                    Read(UiMotionSettings.ButtonMaxDelayMs, 0, 1000), Random.Shared.NextSingle());
                Start(control, UiMotionSettings.Buttons, Read(UiMotionSettings.ButtonEnterMs, 0, 1000), delay / 1000);
            }
        }
        _pending.Clear();

        foreach (var (control, fade) in _active)
        {
            if (!control.IsInsideTree || !control.VisibleInTree || !_configuration.GetCVar(UiMotionSettings.Enabled)
                || !_configuration.GetCVar(fade.Enabled))
            {
                _completed.Add(control);
                continue;
            }

            // An owning control may change its own tint during the transition.
            if (control.Modulate != fade.Applied)
                fade.Original = control.Modulate;
            var delta = args.DeltaSeconds;
            var speed = UiMotionSettings.NormalizeSpeed(_configuration.GetCVar(UiMotionSettings.Speed));
            var waited = Math.Min(fade.Delay, delta * speed);
            fade.Delay -= waited;
            delta -= waited / speed;
            fade.Transition.Advance(delta);
            fade.Applied = fade.Original.WithAlpha(fade.Original.A * fade.Transition.Opacity);
            control.Modulate = fade.Applied;
            if (fade.Transition.Finished)
                _completed.Add(control);
        }

        foreach (var control in _completed)
            Cancel(control);
        _completed.Clear();
    }
}
// Arcane-End
