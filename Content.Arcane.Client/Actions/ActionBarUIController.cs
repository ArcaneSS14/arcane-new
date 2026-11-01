using System.Linq;
using Content.Arcane.Common.Actions;
using Content.Client.Actions;
using Content.Client.UserInterface.Systems.Actions;
using Content.Client.UserInterface.Systems.Actions.Controls;
using Content.Client.UserInterface.Systems.Actions.Widgets;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared.Actions.Components;
using Content.Shared.Input;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;
using Robust.Shared.Input;

namespace Content.Arcane.Client.Actions;

/// <summary>
/// Adapts old Arcane pins and placement to the current action owner without duplicating its action list.
/// </summary>
public sealed partial class ActionBarUIController : UIController, IActionBarCustomization, IOnSystemChanged<PinnedActionsSystem>
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IInputManager _input = default!;

    private readonly ActionPinnedSlots _pins = new();
    private readonly Dictionary<EntProtoId, EntityUid> _placeholders = new();
    private ActionUIController _actions = default!;
    private PinnedActionsSystem? _savedPins;
    private ArcaneActionButtonContainer? _container;
    private ActionBarPlacement? _placement;
    private ActionsBarDragHandle? _grip;

    public bool CanDrag => !_cfg.GetCVar(ActionBarCVars.LockDrag) || _actions.IsActionsMenuOpen;

    public override void Initialize()
    {
        base.Initialize();
        _actions = UIManager.GetUIController<ActionUIController>();
        _actions.Customization = this;
        var screens = UIManager.GetUIController<GameplayStateLoadController>();
        screens.OnScreenLoad += InstallBar;
        screens.OnScreenUnload += UnloadBar;
    }

    public void OnSystemLoaded(PinnedActionsSystem system)
    {
        _savedPins = system;
        system.PinsChanged += _actions.RefreshSlots;
    }

    public void OnSystemUnloaded(PinnedActionsSystem system)
    {
        system.PinsChanged -= _actions.RefreshSlots;
        _savedPins = null;
    }

    private void InstallBar()
    {
        UnloadBar();
        if (UIManager.GetActiveUIWidgetOrNull<ActionsBar>() is not { } bar)
            return;

        _container = new ArcaneActionButtonContainer(this, EntityManager);
        bar.ReplaceActionsContainer(_container);
        _grip = new ActionsBarDragHandle();
        _container.Parent!.AddChild(_grip);
        _grip.SetPositionInParent(0);
        _placement = new ActionBarPlacement(bar, _container, _grip, UIManager.GetUIController<ActionsBarLayoutUIController>());
        _actions.RegisterActionContainer(_container);
        _actions.RefreshSlots();
    }

    private void UnloadBar()
    {
        _placement?.Dispose();
        _placement = null;
        _container = null;
        _grip = null;
    }

    public override void FrameUpdate(Robust.Shared.Timing.FrameEventArgs args)
    {
        base.FrameUpdate(args);
        _placement?.Update();
    }

    public EntProtoId? GetPrototype(EntityUid action)
    {
        if (_pins.GetPrototype(action) is { } pinned)
            return pinned;

        return EntityManager.TryGetComponent<MetaDataComponent>(action, out var metadata)
            ? metadata.EntityPrototype?.ID
            : null;
    }

    public bool IsPinned(EntityUid action) => _pins.IsPinned(action)
        || GetPrototype(action) is { } prototype && _savedPins?.IsPinned(prototype) == true;

    public bool IsUnavailable(EntityUid action)
    {
        return _pins.IsUnavailable(action)
            || !EntityManager.TryGetComponent<ActionComponent>(action, out var component)
            || !component.Enabled || component.AttachedEntity != _player.LocalEntity;
    }

    public bool TryRestoreSlot(Entity<ActionComponent> action, IList<EntityUid?> slots)
    {
        var restored = _pins.TryRestore(action, GetPrototype(action), action.Comp.Container, slots);
        DeleteOrphanedPlaceholders(slots);
        return restored;
    }

    public bool KeepRemovedSlot(EntityUid action)
    {
        if (!_pins.IsPinned(action))
            return false;

        _pins.SetUnavailable(action);
        return true;
    }

    public void RemapSlot(EntityUid previous, EntityUid current) => _pins.Remap(previous, current);

    public void UpdateSlots(IList<EntityUid?> slots)
    {
        if (_savedPins == null)
            return;

        foreach (var action in _pins.GetPinned().ToArray())
        {
            if (GetPrototype(action) is { } prototype && !_savedPins.IsPinned(prototype))
            {
                if (_pins.Unpin(action))
                    slots.Remove(action);
            }
        }

        foreach (var slot in slots)
        {
            if (slot is not { } action || _pins.IsPinned(action)
                || !EntityManager.TryGetComponent<ActionComponent>(action, out var component)
                || GetPrototype(action) is not { } prototype || !_savedPins.IsPinned(prototype))
                continue;

            _pins.Pin(action, prototype, component.Container);
        }

        DeleteOrphanedPlaceholders(slots);
        var represented = new HashSet<EntProtoId>();
        foreach (var slot in slots)
        {
            if (slot is { } action && !_placeholders.ContainsValue(action) && GetPrototype(action) is { } prototype)
                represented.Add(prototype);
        }

        foreach (var (prototype, placeholder) in _placeholders.ToArray())
        {
            if (_savedPins.IsPinned(prototype) && !represented.Contains(prototype))
                continue;

            slots.Remove(placeholder);
            DeletePlaceholder(prototype, placeholder);
        }

        foreach (var (prototype, position) in _savedPins.GetPositions())
        {
            EntityUid? chosen = position < slots.Count && slots[position] is { } preferred && GetPrototype(preferred) == prototype
                ? preferred : null;
            foreach (var slot in slots)
            {
                if (chosen != null)
                    break;
                if (slot is { } action && GetPrototype(action) == prototype)
                    chosen = action;
            }
            if (chosen == null)
            {
                foreach (var action in EntityManager.System<ActionsSystem>().GetClientActions())
                {
                    if (GetPrototype(action) != prototype)
                        continue;
                    chosen = action.Owner;
                    _pins.Pin(action, prototype, action.Comp.Container);
                    break;
                }
            }
            if (chosen == null)
            {
                var placeholder = EntityManager.Spawn();
                _pins.Pin(placeholder, prototype, null);
                _pins.SetUnavailable(placeholder);
                _placeholders[prototype] = placeholder;
                chosen = placeholder;
            }

            for (var i = 0; i < slots.Count; i++)
            {
                if (i != position && slots[i] == chosen)
                    slots[i] = null;
            }
            while (slots.Count <= position)
                slots.Add(null);
            if (slots[position] is { } displaced && displaced != chosen)
                slots.Add(displaced);
            slots[position] = chosen;
        }
        while (slots.Count > 0 && slots[^1] == null)
            slots.RemoveAt(slots.Count - 1);
    }

    public void ClearSlots(IList<EntityUid?> slots)
    {
        foreach (var (prototype, placeholder) in _placeholders.ToArray())
        {
            slots.Remove(placeholder);
            DeletePlaceholder(prototype, placeholder);
        }
        _pins.Clear();
    }

    private void DeleteOrphanedPlaceholders(IList<EntityUid?> slots)
    {
        foreach (var (prototype, placeholder) in _placeholders.ToArray())
        {
            if (!slots.Contains(placeholder))
                DeletePlaceholder(prototype, placeholder);
        }
    }

    private void DeletePlaceholder(EntProtoId prototype, EntityUid placeholder)
    {
        _placeholders.Remove(prototype);
        _pins.Unpin(placeholder);
        if (!EntityManager.Deleted(placeholder))
            EntityManager.DeleteEntity(placeholder);
    }

    public bool HandlePressed(GUIBoundKeyEventArgs args, ActionButton button, IList<EntityUid?> slots)
    {
        if (_container?.TryGetButtonIndex(button, out var index) != true || index >= slots.Count
            || slots[index] is not { } action)
            return false;

        return args.Function == EngineKeyFunctions.UIRightClick && IsPinned(action);
    }

    public void TogglePin(EntityUid action, int position)
    {
        if (GetPrototype(action) is not { } prototype || _savedPins == null)
            return;

        _savedPins.SetPinned(prototype, !_savedPins.IsPinned(prototype), position);
        _actions.RefreshSlots();
    }

    public bool HandleDrop(ActionButton dragged, Control? target, Vector2 mouse, IList<EntityUid?> slots)
    {
        if (_container == null)
            return false;

        if (!_input.IsKeyDown(Keyboard.Key.Shift) && _container.TryHandleSlotDrop(dragged, target, mouse))
            return true;

        // A deliberate slot move must update the saved pin before the owner redraws its action list.
        if (_savedPins != null && _container.TryGetButtonIndex(dragged, out var source) && source < slots.Count
            && slots[source] is { } moved)
        {
            var movedPrototype = GetPrototype(moved);
            if (target is ActionButton other && _container.TryGetButtonIndex(other, out var destination))
            {
                var replaced = destination < slots.Count ? slots[destination] : null;
                var replacedPrototype = replaced is { } uid ? GetPrototype(uid) : null;
                if (movedPrototype is { } moving && _savedPins.IsPinned(moving)
                    || replacedPrototype is { } replacing && _savedPins.IsPinned(replacing))
                {
                    while (slots.Count <= destination)
                        slots.Add(null);
                    slots[source] = replaced;
                    slots[destination] = moved;
                    if (movedPrototype is { } from && _savedPins.IsPinned(from))
                        _savedPins.SetPinned(from, true, destination, notify: false);
                    if (replacedPrototype is { } to && _savedPins.IsPinned(to))
                        _savedPins.SetPinned(to, true, source, notify: false);
                    return true;
                }
            }
            else if (movedPrototype is { } removed && _savedPins.IsPinned(removed))
            {
                _savedPins.SetPinned(removed, false, notify: false);
                slots[source] = null;
                return true;
            }
        }

        if (target != _grip || dragged.Parent == _container || dragged.Action is not { } action)
            return false;

        if (!slots.Contains(action.Owner))
            slots.Add(action.Owner);
        return true;
    }

    public void UpdateDragHint(ActionButton? dragged, Vector2 mouse) => _container?.UpdateReturnHint(dragged, mouse);
}
