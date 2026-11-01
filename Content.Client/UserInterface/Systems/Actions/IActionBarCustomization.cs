// Arcane-Start
using System.Numerics;
using Content.Client.UserInterface.Systems.Actions.Controls;
using Content.Shared.Actions.Components;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Systems.Actions;

/// <summary>
/// Optional content-side customization of the existing action list and drag behavior.
/// </summary>
public interface IActionBarCustomization
{
    bool TryRestoreSlot(Entity<ActionComponent> action, IList<EntityUid?> slots);
    bool KeepRemovedSlot(EntityUid action);
    void UpdateSlots(IList<EntityUid?> slots);
    void ClearSlots(IList<EntityUid?> slots);
    void RemapSlot(EntityUid previous, EntityUid current);
    bool IsUnavailable(EntityUid action);
    bool HandlePressed(GUIBoundKeyEventArgs args, ActionButton button, IList<EntityUid?> slots);
    bool HandleDrop(ActionButton dragged, Control? target, Vector2 mouse, IList<EntityUid?> slots);
    void UpdateDragHint(ActionButton? dragged, Vector2 mouse);
    bool CanDrag { get; }
}
// Arcane-End
