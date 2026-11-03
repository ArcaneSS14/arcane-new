using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Trauma.Common.Inventory;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

/// <summary>
/// Arcane - underwear is clothing (Orion #208), so a preview without job clothes still wears it.
/// </summary>
public sealed partial class ProfilePreviewSpriteView
{
    private static readonly ProtoId<InventorySlotPrototype>[] UnderwearSlots = ["underwear", "undershirt", "socks", "wrists"];

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

                // Same order as StationSpawningSystem.EquipStartingGear(LoadoutPrototype).
                if (_prototypeManager.Resolve(loadoutProto.StartingGear, out var startingGear))
                    EquipUnderwear(inventory, startingGear);

                EquipUnderwear(inventory, loadoutProto);
            }
        }
    }

    private void EquipUnderwear(InventorySystem inventory, IEquipmentLoadout gear)
    {
        foreach (var slot in UnderwearSlots)
        {
            var itemType = gear.GetGear(slot);
            if (string.IsNullOrEmpty(itemType))
                continue;

            var item = EntMan.SpawnEntity(itemType, MapCoordinates.Nullspace);
            if (!inventory.TryEquip(PreviewDummy, item, slot, silent: true, force: true))
                EntMan.DeleteEntity(item);
        }
    }
}
