using Content.Arcane.Common.CCVar;
using Content.Arcane.Shared.Security;
using Content.Client.Inventory;
using Content.Client.Overlays;
using Content.Goobstation.Shared.Contraband;
using Content.Shared.Contraband;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Configuration;

namespace Content.Arcane.Client.Security;

public sealed partial class ContrabandHudSystem : EquipmentHudSystem<ContrabandHudComponent>
{
    [Dependency] private IConfigurationManager _configuration = default!;
    [Dependency] private ContrabandSystem _contraband = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private static readonly ProtoId<StatusIconPrototype> ContrabandIcon = "ContrabandIconContraband";
    private static readonly ProtoId<StatusIconPrototype> ClearIcon = "ContrabandIconNone";
    private bool _enabled;

    protected override SlotFlags TargetSlots => SlotFlags.EYES;

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_configuration, ArcaneCVars.ContrabandIconsEnabled, enabled => _enabled = enabled, true);
    }

    [SubscribeLocalEvent]
    private void OnGetIcons(Entity<InventoryComponent> ent, ref GetStatusIconsEvent args)
    {
        if (!_enabled || !IsActive || _playerManager.LocalEntity is not { } viewer || viewer == ent.Owner ||
            !_examine.CanExamine(viewer, ent))
            return;

        // This status uses the same FoV-masked overlay as the other status icons. Inspect only
        // equipment whose sprite is already visible on this client, and items in actual hands.
        args.StatusIcons.Add(ProtoMan.Index(HasVisibleContraband(ent) ? ContrabandIcon : ClearIcon));
    }

    private bool HasVisibleContraband(Entity<InventoryComponent> ent)
    {
        foreach (var held in _hands.EnumerateHeld(ent.Owner))
        {
            if (IsContraband(held, ent))
                return true;
        }

        if (!TryComp<InventorySlotsComponent>(ent, out var inventoryVisuals) || !TryComp<SpriteComponent>(ent, out var sprite))
            return false;

        var slots = _inventory.GetSlotEnumerator(ent.AsNullable(), SlotFlags.WITHOUT_POCKET & ~SlotFlags.IDCARD);
        while (slots.NextItem(out var item, out var slot))
        {
            if (!inventoryVisuals.VisualLayerKeys.TryGetValue(slot.Name, out var keys))
                continue;

            foreach (var key in keys)
            {
                if (_sprite.LayerMapTryGet((ent.Owner, sprite), key, out var index, false) &&
                    sprite[index].Visible && IsContraband(item, ent))
                    return true;
            }
        }
        return false;
    }

    private bool IsContraband(EntityUid item, EntityUid carrier) =>
        !HasComp<UndetectableContrabandComponent>(item) && _contraband.IsContraband(item, carrier, out _);
}
