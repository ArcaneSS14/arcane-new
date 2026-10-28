// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Inventory.Controls;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Utility;

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

        foreach (var child in new List<Control>(_extraHotbar.Children))
        {
            if (child is not SlotControl)
                _extraHotbar.RemoveChild(child);
        }

        var maxWidth = extra.Max(p => p.Value.ButtonOffset.X) + 1;
        var maxIndex = extra.Max(p => GetIndex(p.Value.ButtonOffset));

        _extraHotbar.MaxColumns = maxWidth;
        _extraHotbar.Columns = maxWidth;

        for (var i = 0; i <= maxIndex; i++)
        {
            var index = i;
            if (extra.FirstOrNull(p => GetIndex(p.Value.ButtonOffset) == index) is { } pair)
            {
                if (_extraHotbar.TryGetButton(pair.Key, out var slot))
                    slot.SetPositionLast();
            }
            else
            {
                _extraHotbar.AddChild(new Control { MinSize = new Vector2(64, 64) });
            }
        }

        int GetIndex(Vector2i position)
        {
            return position.Y * maxWidth + position.X;
        }
    }
}
