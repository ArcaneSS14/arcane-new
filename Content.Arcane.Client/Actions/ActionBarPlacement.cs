using Content.Client.UserInterface.Systems.Actions.Widgets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Arcane.Client.Actions;

/// <summary>
/// Keeps a flow-layout anchor in the current HUD while positioning the bar in its screen's layout space.
/// </summary>
public sealed class ActionBarPlacement : IDisposable
{
    private readonly ActionsBar _bar;
    private readonly ArcaneActionButtonContainer _container;
    private readonly ActionsBarDragHandle _grip;
    private readonly ActionsBarLayoutUIController _layout;
    private readonly LayoutContainer? _space;
    private readonly Control _anchor;
    private Vector2 _grabOffset;

    public ActionBarPlacement(ActionsBar bar, ArcaneActionButtonContainer container,
        ActionsBarDragHandle grip, ActionsBarLayoutUIController layout)
    {
        _bar = bar;
        _container = container;
        _grip = grip;
        _layout = layout;
        var parent = bar.Parent!;
        var index = bar.GetPositionInParent();
        _anchor = new Control { MinSize = bar.DesiredSize, MouseFilter = Control.MouseFilterMode.Ignore };
        UIScreen? screen = null;
        for (Control? ancestor = parent; ancestor != null; ancestor = ancestor.Parent)
        {
            if (_space == null && ancestor is LayoutContainer space)
                _space = space;
            if (ancestor is UIScreen currentScreen)
            {
                screen = currentScreen;
                break;
            }
        }

        if (_space != null && screen != null)
        {
            // A screen also tracks nested widgets; a plain Orphan leaves its registry entry behind.
            screen.RemoveWidget<ActionsBar>();
            parent.AddChild(_anchor);
            _anchor.SetPositionInParent(index);
            _space.AddChild(bar);
            if (_space != screen)
                screen.AddWidgetDirect(bar);
            container.PositionSpace = _space;
        }

        container.ReturnHintChanged += grip.SetHighlighted;
        grip.DragStarted += OnDragStarted;
        grip.Dragged += OnDragged;
        grip.ResetRequested += layout.Reset;
    }

    public void Dispose()
    {
        _container.ReturnHintChanged -= _grip.SetHighlighted;
        _grip.DragStarted -= OnDragStarted;
        _grip.Dragged -= OnDragged;
        _grip.ResetRequested -= _layout.Reset;
    }

    public void Update()
    {
        if (_bar.Disposed || _space == null || _space.Disposed)
            return;

        _anchor.MinSize = _bar.DesiredSize;
        var position = _layout.BarPosition is { } custom
            ? Clamp(custom)
            : _anchor.GlobalPosition - _space.GlobalPosition;
        _container.SetBarPlacement(_bar, position);
        if (_bar.GetValue<float>(LayoutContainer.MarginLeftProperty).Equals(position.X)
            && _bar.GetValue<float>(LayoutContainer.MarginTopProperty).Equals(position.Y))
            return;

        LayoutContainer.SetAnchorPreset(_bar, LayoutContainer.LayoutPreset.TopLeft);
        LayoutContainer.SetMarginLeft(_bar, position.X);
        LayoutContainer.SetMarginRight(_bar, position.X);
        LayoutContainer.SetMarginTop(_bar, position.Y);
        LayoutContainer.SetMarginBottom(_bar, position.Y);
    }

    private Vector2 Clamp(Vector2 position) => _space == null ? position
        : Vector2.Clamp(position, Vector2.Zero, Vector2.Max(Vector2.Zero, _space.Size - _bar.Size));

    private void OnDragStarted(Vector2 mouse) => _grabOffset = mouse - _bar.GlobalPosition;

    private void OnDragged(Vector2 mouse)
    {
        if (_space != null)
            _layout.SetBarPosition(Clamp(mouse - _grabOffset - _space.GlobalPosition));
    }
}
