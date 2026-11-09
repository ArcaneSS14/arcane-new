using System.Linq;
using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Inventory.Controls;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Utility;
using static Content.Client.Inventory.ClientInventorySystem;

namespace Content.Client.UserInterface.Systems.Inventory;

/// <summary>
/// Arcane - second hotbar for the underwear slots, toggled next to the inventory bar (Orion #208).
/// </summary>
public sealed partial class InventoryUIController
{
    public const string ExtraSlotGroup = "Extra";

    private ItemSlotButtonContainer? _extraHotbar;
    private SlotButton? _extraButton;

    public void RegisterExtraHotbar(ItemSlotButtonContainer hotbar, SlotButton button)
    {
        if (_extraButton != null)
            _extraButton.Pressed -= ExtraButtonPressed;

        _extraHotbar = hotbar;
        _extraButton = button;
        _extraButton.Pressed += ExtraButtonPressed;
    }

    private void ExtraButtonPressed(GUIBoundKeyEventArgs args, SlotControl control)
    {
        if (args.Function != EngineKeyFunctions.UIClick || _extraHotbar == null)
            return;

        _extraHotbar.Visible = !_extraHotbar.Visible;
        UpdateExtraHotbar();
        args.Handle();
    }

    /// <summary>
    /// The extra bar can only be opened while the inventory bar is open.
    /// </summary>
    private void UpdateExtraHotbar()
    {
        if (_extraHotbar == null)
            return;

        var extra = _playerInventory?.SlotData
            .Where(p => p.Value.SlotGroup == ExtraSlotGroup && p.Value.ShowInWindow)
            .ToList() ?? [];

        var available = extra.Count != 0 && _inventoryHotbar is { Visible: true };
        if (_extraButton != null)
            _extraButton.Visible = available;

        if (!available)
        {
            _extraHotbar.Visible = false;
            return;
        }

        LayoutHotbar(_extraHotbar, extra);
    }

    /// <summary>
    /// Lays the slots out in a grid by their button offsets, filling the gaps with empty cells.
    /// Shared by the inventory hotbar and the extra hotbar so both keep the same layout rules.
    /// </summary>
    private static void LayoutHotbar(ItemSlotButtonContainer hotbar, List<KeyValuePair<string, SlotData>> slots)
    {
        foreach (var child in new List<Control>(hotbar.Children))
        {
            if (child is not SlotControl)
                hotbar.RemoveChild(child);
        }

        var maxWidth = slots.Max(p => p.Value.ButtonOffset.X) + 1;
        var byIndex = new Dictionary<int, string>();
        foreach (var (name, data) in slots)
        {
            byIndex.TryAdd(data.ButtonOffset.Y * maxWidth + data.ButtonOffset.X, name);
        }

        hotbar.MaxColumns = maxWidth;
        hotbar.Columns = maxWidth;

        var maxIndex = byIndex.Keys.Max();
        for (var i = 0; i <= maxIndex; i++)
        {
            if (byIndex.TryGetValue(i, out var name))
            {
                if (hotbar.TryGetButton(name, out var slot))
                    slot.SetPositionLast();
            }
            else
            {
                hotbar.AddChild(new Control { MinSize = new Vector2(64, 64) });
            }
        }
    }
}
