// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Shared.Map;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

/// <summary>
/// Arcane - underwear is clothing (Orion #208), so a preview without job clothes still wears it.
/// </summary>
public sealed partial class ProfilePreviewSpriteView
{
    private static readonly string[] UnderwearSlots = ["underwear", "undershirt", "socks", "wrists"];

    private void GiveDummyUnderwear(HumanoidCharacterProfile profile, JobPrototype job)
    {
        var roleLoadout = LoadoutSystem.GetJobPrototype(job.ID);
        if (!_prototypeManager.HasIndex<RoleLoadoutPrototype>(roleLoadout))
            return;

        var inventory = EntMan.System<InventorySystem>();
        var loadout = profile.GetLoadoutOrDefault(roleLoadout, _playerManager.LocalSession, profile.Species, EntMan, _prototypeManager);
        foreach (var group in loadout.SelectedLoadouts.Values)
        {
            foreach (var selected in group)
            {
                if (!_prototypeManager.Resolve(selected.Prototype, out var loadoutProto))
                    continue;

                foreach (var slot in UnderwearSlots)
                {
                    var itemType = ((IEquipmentLoadout) loadoutProto).GetGear(slot);
                    if (string.IsNullOrEmpty(itemType))
                        continue;

                    var item = EntMan.SpawnEntity(itemType, MapCoordinates.Nullspace);
                    if (!inventory.TryEquip(PreviewDummy, item, slot, silent: true, force: true))
                        EntMan.DeleteEntity(item);
                }
            }
        }
    }
}
