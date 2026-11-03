// Arcane-Start
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Content.Client.ContextMenu.UI;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Actions.Controls;
using Content.Client.UserInterface.Systems.Alerts.Controls;
using Content.Client.UserInterface.Systems.Storage.Controls;
using Content.Client.Administration.UI.CustomControls;
using Content.Client.Administration.UI.Tabs.PlayerTab;
using TreeItem = Content.Client.UserInterface.Controls.FancyTree.TreeItem;

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
    private readonly Dictionary<Control, int> _indices = [];

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
        Attach(UIManager.PopupRoot);
        Attach(UIManager.ModalRoot);
        Attach(UIManager.StateRoot);
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
        if (control.VisibleInTree && (control is BaseWindow or BaseButton or Popup or ActionButton or ItemGridPiece
            || control.Parent is TabContainer
            || IsListRow(control) || control.Parent == UIManager.PopupRoot) && !_pending.Contains(control))
            _pending.Add(control);
    }

    /// <summary>Queue a changed content control in its parent's appearance sequence.</summary>
    public void QueueAppearance(Control control) => Queue(control);

    private static bool IsListRow(Control control) => control is PlayerListEntry or PlayerTabEntry or UiMotionListRow
        || control.Parent?.Parent is TreeItem item && item.Button == control;

    private bool IsInsideAnimatedParent(Control control)
    {
        for (var parent = control.Parent; parent != null; parent = parent.Parent)
        {
            if (parent is Popup or TabContainer or UiMotionContainer { Profile.Opening: true }
                || parent.Parent == UIManager.PopupRoot || IsListRow(parent))
                return true;
        }
        return false;
    }

    private void CheckboxToggled(BaseButton.ButtonToggledEventArgs args)
    {
        if (args.Button is not CheckBox checkbox || _configuration.GetCVar(UiMotionSettings.Reduce))
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
        if (!UiMotionSettings.IsEnabled(_configuration, enabled) || milliseconds <= 0)
            return;

        var transition = new UiMotionTransition(_configuration);
        transition.Start(UiMotionSettings.Resolve(_configuration,
            new UiMotionProfile(TimeSpan.FromMilliseconds(milliseconds), TimeSpan.Zero, 0, false)), true, from, 0);
        var fade = new Fade
        {
            Transition = transition,
            Enabled = enabled,
            Original = control.Modulate,
            Applied = control.Modulate.WithAlpha(control.Modulate.A * from),
            Delay = _configuration.GetCVar(UiMotionSettings.Reduce) ? 0 : delay
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
        _indices.Clear();
        foreach (var control in _pending)
        {
            if (!control.IsInsideTree || !control.VisibleInTree || control is ContextMenuPopup || IsInsideAnimatedParent(control))
                continue;
            var parent = control.Parent?.Parent is TreeItem tree && tree.Button == control
                ? tree.Parent! : control.Parent!;
            _indices.TryGetValue(parent, out var index);
            _indices[parent] = index + 1;
            if (control is Popup || control.Parent == UIManager.PopupRoot)
                Start(control, UiMotionSettings.Popups, Read(UiMotionSettings.PopupsMs, 0, 1000), 0);
            else if (control is RadialMenu)
                continue; // Its sectors own the entrance; do not multiply their opacity by a second fade.
            else if (control is AlertControl alert)
                alert.AnimateAppearance();
            else if (control is ItemGridPiece piece)
                piece.AnimateAppearance();
            else if (control is BaseWindow)
                Start(control, UiMotionSettings.Windows, Read(UiMotionSettings.WindowEnterMs, 0, 1000), 0);
            else if (control.Parent is TabContainer)
                Start(control, UiMotionSettings.Tabs, Read(UiMotionSettings.TabsEnterMs, 0, 1000), 0);
            else
            {
                var row = IsListRow(control);
                var radial = control is RadialMenuButtonBase or RadialMenuContextualCentralTextureButton;
                var group = row ? UiMotionSettings.Lists : radial ? UiMotionSettings.Radial
                    : control is ActionButton ? UiMotionSettings.Actions : UiMotionSettings.Buttons;
                var duration = row ? UiMotionSettings.ListsEnterMs : radial ? UiMotionSettings.RadialEnterMs
                    : control is ActionButton ? UiMotionSettings.ActionsEnterMs : UiMotionSettings.ButtonEnterMs;
                if (row)
                    index = Math.Min(index, (int) Read(UiMotionSettings.ListsMaxStagger, 0, 32));
                var delay = UiMotionSequence.Delay(index, radial ? 1 : _configuration.GetCVar(UiMotionSettings.ButtonOrder),
                    Read(UiMotionSettings.ButtonIntervalMs, 0, 150), Read(UiMotionSettings.ButtonJitterMs, 0, 150),
                    Read(UiMotionSettings.ButtonMaxDelayMs, 0, 1000), Random.Shared.NextSingle());
                if (control is ActionButton action)
                    action.AnimateAppearance(delay / 1000);
                else
                    Start(control, group, Read(duration, 0, 1000), delay / 1000);
            }
        }
        _pending.Clear();

        foreach (var (control, fade) in _active)
        {
            if (!control.IsInsideTree || !control.VisibleInTree || !_configuration.GetCVar(UiMotionSettings.Enabled)
                || !_configuration.GetCVar(fade.Enabled)
                || fade.Enabled == UiMotionSettings.Checkboxes && _configuration.GetCVar(UiMotionSettings.Reduce))
            {
                _completed.Add(control);
                continue;
            }

            // An owning control may change its own tint during the transition.
            if (control.Modulate != fade.Applied)
                fade.Original = control.Modulate;
            var delta = args.DeltaSeconds;
            var speed = UiMotionSettings.PlaybackSpeed(_configuration);
            if (_configuration.GetCVar(UiMotionSettings.Reduce))
                fade.Delay = 0;
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
