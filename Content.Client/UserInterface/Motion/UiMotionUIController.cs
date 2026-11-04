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
using System.Numerics;
using Content.Client.Mapping;

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
    private readonly HashSet<Control> _pendingSet = [];
    private readonly Dictionary<Control, Fade> _active = [];
    private readonly List<Control> _completed = [];
    private readonly Dictionary<Control, int> _indices = [];
    private readonly Dictionary<BaseButton, ButtonFeedback> _buttonFeedback = [];
    private readonly HashSet<BaseButton> _activeButtons = [];
    private readonly List<BaseButton> _completedButtons = [];

    private sealed class Fade
    {
        public required UiMotionTransition Transition;
        public required CVarDef<bool> Enabled;
        public Color Original;
        public Color Applied;
        public float Delay;
        public Vector2? Origin;
        public Vector2? AppliedPosition;
        public bool PopupPosition;
        public bool SuppressChildren;
    }

    private sealed class ButtonFeedback
    {
        public readonly UiMotionSpring Hover = new();
        public readonly UiMotionSpring Press = new();
        public Color Original;
        public Color Applied;
        public bool Hovered;
        public bool Pressed;
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
        if (control is BaseButton button)
        {
            button.OnMouseEntered += ButtonMouseEntered;
            button.OnMouseExited += ButtonMouseExited;
            button.OnButtonDown += ButtonDown;
            button.OnButtonUp += ButtonUp;
            if (button is CheckBox or SwitchButton)
                button.OnToggled += ToggleToggled;
        }
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
        if (control is BaseButton button)
        {
            button.OnMouseEntered -= ButtonMouseEntered;
            button.OnMouseExited -= ButtonMouseExited;
            button.OnButtonDown -= ButtonDown;
            button.OnButtonUp -= ButtonUp;
            if (button is CheckBox or SwitchButton)
                button.OnToggled -= ToggleToggled;
            ClearButtonFeedback(button);
        }
        RemovePending(control);
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
        if (control is AlertControl)
            return;
        if (control.VisibleInTree && (control is BaseWindow or BaseButton or Popup or ActionButton or ItemGridPiece
            || control.Parent is TabContainer
            || IsListRow(control) || control.Parent == UIManager.PopupRoot) && _pendingSet.Add(control))
            _pending.Add(control);
    }

    private void RemovePending(Control control)
    {
        if (_pendingSet.Remove(control))
            _pending.Remove(control);
    }

    /// <summary>Queue a changed content control in its parent's appearance sequence.</summary>
    public void QueueAppearance(Control control) => Queue(control);

    /// <summary>Animate one already-visible panel after its owner switches content or visibility.</summary>
    public void AnimatePanel(Control control)
    {
        if (!control.VisibleInTree)
            return;
        RemovePending(control);
        Start(control, UiMotionSettings.Tabs, Read(UiMotionSettings.TabsEnterMs, 0, 1000), 0,
            suppressChildren: true);
    }

    /// <summary>Animate an explicitly opened popup without waiting for tree observation.</summary>
    public void AnimatePopup(Control control)
    {
        if (!control.VisibleInTree)
            return;
        RemovePending(control);
        Start(control, UiMotionSettings.Popups, Read(UiMotionSettings.PopupsMs, 0, 1000), 0,
            suppressChildren: true);
    }

    /// <summary>Animate an embedded window without changing layout-managed coordinates.</summary>
    public void AnimateWindow(Control control)
    {
        if (!control.VisibleInTree)
            return;
        RemovePending(control);
        Start(control, UiMotionSettings.Windows, Read(UiMotionSettings.WindowEnterMs, 0, 1000), 0,
            suppressChildren: true);
    }

    /// <summary>Animate only a newly-added or replaced list row.</summary>
    public void AnimateListItem(Control control, int index = 0)
    {
        if (!control.VisibleInTree)
            return;
        RemovePending(control);
        var delay = UiMotionSequence.Delay(index, 1,
            Read(UiMotionSettings.ButtonIntervalMs, 0, 150), 0,
            Read(UiMotionSettings.ButtonMaxDelayMs, 0, 1000), 0) / 1000;
        Start(control, UiMotionSettings.Lists, Read(UiMotionSettings.ListsEnterMs, 0, 1000), delay);
    }

    /// <summary>Animate a row after its owner has completed insertion and visibility changes for this frame.</summary>
    public void AnimateListItemDeferred(Control control, int index = 0)
    {
        UIManager.DeferAction(() =>
        {
            if (control.IsInsideTree && control.VisibleInTree)
                AnimateListItem(control, index);
        });
    }

    private static bool IsListRow(Control control) => control is PlayerListEntry or PlayerTabEntry or UiMotionListRow
        or EntitySpawnButton or MappingSpawnButton
        || control.Parent?.Parent is TreeItem item && item.Button == control;

    private bool IsInsideAnimatedParent(Control control)
    {
        for (var parent = control.Parent; parent != null; parent = parent.Parent)
        {
            if (_active.TryGetValue(parent, out var activeParent) && activeParent.SuppressChildren)
                return true;
            if (parent is Popup popup)
            {
                if (!IsDropdownPopup(popup))
                    return true;
                continue;
            }
            if (parent is TabContainer && parent != control.Parent
                || parent is UiMotionContainer { Profile.Opening: true }
                || parent.Parent == UIManager.PopupRoot || IsListRow(parent))
                return true;
        }
        return false;
    }

    private static bool IsDropdownPopup(Control control) => control is Popup
        && control.StyleClasses.Contains(OptionButton.StyleClassPopup);

    private static bool IsInsideDropdownPopup(Control control)
    {
        for (var parent = control.Parent; parent != null; parent = parent.Parent)
        {
            if (parent is Popup)
                return IsDropdownPopup(parent);
        }
        return false;
    }

    private void ButtonMouseEntered(GUIMouseHoverEventArgs args)
    {
        if (args.SourceControl is BaseButton button)
            SetButtonFeedback(button, hovered: true);
    }

    private void ButtonMouseExited(GUIMouseHoverEventArgs args)
    {
        if (args.SourceControl is BaseButton button)
            SetButtonFeedback(button, hovered: false, pressed: false);
    }

    private void ButtonDown(BaseButton.ButtonEventArgs args) => SetButtonFeedback(args.Button, pressed: true);

    private void ButtonUp(BaseButton.ButtonEventArgs args) => SetButtonFeedback(args.Button, pressed: false);

    private void SetButtonFeedback(BaseButton button, bool? hovered = null, bool? pressed = null)
    {
        if (!_buttonFeedback.TryGetValue(button, out var feedback))
        {
            feedback = new ButtonFeedback
            {
                Original = button.Modulate,
                Applied = button.Modulate
            };
            _buttonFeedback.Add(button, feedback);
        }
        if (hovered is { } hover)
            feedback.Hovered = hover;
        if (pressed is { } press)
            feedback.Pressed = press;
        _activeButtons.Add(button);
    }

    private void ClearButtonFeedback(BaseButton button)
    {
        _activeButtons.Remove(button);
        if (!_buttonFeedback.Remove(button, out var feedback))
            return;
        if (button.Modulate == feedback.Applied)
            button.Modulate = feedback.Original;
    }

    private void ToggleToggled(BaseButton.ButtonToggledEventArgs args)
    {
        var visual = args.Button switch
        {
            CheckBox checkbox => checkbox.TextureRect,
            SwitchButton switchButton => switchButton.ThumbFill,
            _ => null
        };
        if (visual == null || _configuration.GetCVar(UiMotionSettings.Reduce))
            return;
        Start(visual, UiMotionSettings.Checkboxes,
            Read(UiMotionSettings.CheckboxMs, 0, 1000), 0,
            1 - Read(UiMotionSettings.CheckboxIntensity, 0, 0.8f));
    }

    private float Read(CVarDef<float> setting, float min, float max) => UiMotionSettings.Read(_configuration, setting, min, max);

    private void Start(Control control, CVarDef<bool> enabled, float milliseconds, float delay, float from = 0,
        float travel = 0, bool expressive = false, bool popupPosition = false, bool suppressChildren = false)
    {
        // Restart from the currently displayed opacity when toggling again.
        if (_active.TryGetValue(control, out var previous))
            from = previous.Transition.Opacity;
        Cancel(control);
        if (!UiMotionSettings.IsEnabled(_configuration, enabled) || milliseconds <= 0)
            return;

        var transition = new UiMotionTransition(_configuration);
        var profile = new UiMotionProfile(TimeSpan.FromMilliseconds(milliseconds), TimeSpan.Zero, travel, expressive)
        {
            EnterEffectDuration = travel > 0 ? UiMotionSettings.SpatialEffectDuration(milliseconds) : null
        };
        transition.Start(UiMotionSettings.Resolve(_configuration, profile), true, from, -travel);
        var fade = new Fade
        {
            Transition = transition,
            Enabled = enabled,
            Original = control.Modulate,
            Applied = control.Modulate.WithAlpha(control.Modulate.A * from),
            Delay = _configuration.GetCVar(UiMotionSettings.Reduce) ? 0 : delay,
            PopupPosition = popupPosition,
            SuppressChildren = suppressChildren
        };
        if (travel > 0 && !_configuration.GetCVar(UiMotionSettings.Reduce))
        {
            fade.Origin = GetPosition(control, popupPosition);
            fade.AppliedPosition = ApplyPosition(control, fade.Origin.Value, transition.Offset, popupPosition);
        }
        _active.Add(control, fade);
        control.Modulate = fade.Applied;
    }

    private void Cancel(Control control)
    {
        if (!_active.Remove(control, out var fade))
            return;
        if (control.Modulate == fade.Applied)
            control.Modulate = fade.Original;
        if (fade.Origin is { } origin && fade.AppliedPosition is { } applied
            && GetPosition(control, fade.PopupPosition) == applied)
            ApplyPosition(control, origin, 0, fade.PopupPosition);
    }

    private static Vector2 GetPosition(Control control, bool popupPosition)
    {
        return popupPosition ? PopupContainer.GetPopupOrigin(control) : control.Position;
    }

    private static Vector2 ApplyPosition(Control control, Vector2 origin, float offset, bool popupPosition)
    {
        var position = origin + new Vector2(0, offset);
        if (control.Parent is { } parent)
            position = Vector2.Clamp(position, Vector2.Zero, Vector2.Max(Vector2.Zero, parent.Size - control.Size));
        if (popupPosition)
            PopupContainer.SetPopupOrigin(control, position);
        else
            LayoutContainer.SetPosition(control, position);
        return position;
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
            if (IsDropdownPopup(control))
                Start(control, UiMotionSettings.Dropdowns, Read(UiMotionSettings.DropdownEnterMs, 0, 1000), 0,
                    travel: Read(UiMotionSettings.DropdownTravel, 0, 24), expressive: true, popupPosition: true);
            else if (control is Popup || control.Parent == UIManager.PopupRoot)
                Start(control, UiMotionSettings.Popups, Read(UiMotionSettings.PopupsMs, 0, 1000), 0);
            else if (control is RadialMenu)
                continue; // Its sectors own the entrance; do not multiply their opacity by a second fade.
            else if (control is ItemGridPiece piece)
                piece.AnimateAppearance();
            else if (control is BaseWindow)
            {
                var travel = control.Parent == UIManager.WindowRoot
                    ? Read(UiMotionSettings.WindowTravel, 0, 24)
                    : 0;
                Start(control, UiMotionSettings.Windows, Read(UiMotionSettings.WindowEnterMs, 0, 1000), 0,
                    travel: travel, expressive: travel > 0);
            }
            else if (control.Parent is TabContainer)
                Start(control, UiMotionSettings.Tabs, Read(UiMotionSettings.TabsEnterMs, 0, 1000), 0,
                    suppressChildren: true);
            else
            {
                var row = IsListRow(control);
                var radial = control is RadialMenuButtonBase or RadialMenuContextualCentralTextureButton;
                var dropdownItem = control is BaseButton && IsInsideDropdownPopup(control);
                var group = dropdownItem ? UiMotionSettings.Dropdowns : row ? UiMotionSettings.Lists : radial ? UiMotionSettings.Radial
                    : control is ActionButton ? UiMotionSettings.Actions : UiMotionSettings.Buttons;
                var duration = dropdownItem ? UiMotionSettings.DropdownItemMs : row ? UiMotionSettings.ListsEnterMs : radial ? UiMotionSettings.RadialEnterMs
                    : control is ActionButton ? UiMotionSettings.ActionsEnterMs : UiMotionSettings.ButtonEnterMs;
                if (row)
                    index = Math.Min(index, (int) Read(UiMotionSettings.ListsMaxStagger, 0, 32));
                var delay = UiMotionSequence.Delay(index, radial || dropdownItem ? 1 : _configuration.GetCVar(UiMotionSettings.ButtonOrder),
                    Read(UiMotionSettings.ButtonIntervalMs, 0, 150), Read(UiMotionSettings.ButtonJitterMs, 0, 150),
                    Read(UiMotionSettings.ButtonMaxDelayMs, 0, 1000), Random.Shared.NextSingle());
                if (control is ActionButton action)
                    action.AnimateAppearance(delay / 1000);
                else
                    Start(control, group, Read(duration, 0, 1000), delay / 1000);
            }
        }
        _pending.Clear();
        _pendingSet.Clear();

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
            var currentPosition = GetPosition(control, fade.PopupPosition);
            if (fade.Origin is { } origin && fade.AppliedPosition is { } applied && currentPosition != applied)
                fade.Origin = origin + currentPosition - applied;
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
            if (fade.Origin is { } motionOrigin)
                fade.AppliedPosition = ApplyPosition(control, motionOrigin, fade.Transition.Offset, fade.PopupPosition);
            if (fade.Transition.Finished)
                _completed.Add(control);
        }

        foreach (var control in _completed)
            Cancel(control);
        _completed.Clear();

        UpdateButtonFeedback(args.DeltaSeconds);
    }

    private void UpdateButtonFeedback(float deltaSeconds)
    {
        foreach (var button in _activeButtons)
        {
            if (!button.IsInsideTree || !button.VisibleInTree || button.Disabled
                || !UiMotionSettings.IsEnabled(_configuration, UiMotionSettings.Buttons)
                || _configuration.GetCVar(UiMotionSettings.Reduce))
            {
                _completedButtons.Add(button);
                continue;
            }
            if (_active.ContainsKey(button))
                continue;

            var feedback = _buttonFeedback[button];
            if (button.Modulate != feedback.Applied)
                feedback.Original = button.Modulate;
            var speed = UiMotionSettings.PlaybackSpeed(_configuration);
            var hover = feedback.Hover.Advance(
                feedback.Hovered && _configuration.GetCVar(UiMotionSettings.ButtonHover) ? 1 : 0,
                deltaSeconds * speed, 1, 1600);
            var press = feedback.Press.Advance(
                feedback.Pressed && _configuration.GetCVar(UiMotionSettings.ButtonPress) ? 1 : 0,
                deltaSeconds * speed, 1, 3800);
            feedback.Applied = ApplyButtonTint(feedback.Original, hover, press);
            button.Modulate = feedback.Applied;
            if (!feedback.Hovered && !feedback.Pressed && feedback.Hover.Value == 0 && feedback.Press.Value == 0)
                _completedButtons.Add(button);
        }

        foreach (var button in _completedButtons)
            ClearButtonFeedback(button);
        _completedButtons.Clear();
    }

    private Color ApplyButtonTint(Color original, float hover, float press)
    {
        var hoverAmount = Math.Clamp(hover, 0, 1)
            * Read(UiMotionSettings.ButtonHoverIntensity, 0, 0.2f);
        var pressAmount = Math.Clamp(press, 0, 1)
            * Read(UiMotionSettings.ButtonPressIntensity, 0, 0.2f);
        const float hoverRed = 0.65f;
        const float hoverGreen = 0.8f;
        const float hoverBlue = 1f;
        var dim = 1 - pressAmount;
        return new Color(
            (original.R + (hoverRed - original.R) * hoverAmount) * dim,
            (original.G + (hoverGreen - original.G) * hoverAmount) * dim,
            (original.B + (hoverBlue - original.B) * hoverAmount) * dim,
            original.A);
    }
}
// Arcane-End
