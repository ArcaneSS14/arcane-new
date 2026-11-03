using System.Linq;
using System.Numerics;
using Content.Arcane.Server.Bitrunning.Components;
using Content.Server.Actions;
using Content.Server.Popups;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Storage;
using Content.Arcane.Shared.Bitrunning;
using Content.Arcane.Shared.Bitrunning.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.Bitrunning.Systems;

public sealed partial class BitrunningDiskSystem : EntitySystem
{
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private StorageSystem _storage = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private ILocalizationManager _loc = default!;

    private readonly Dictionary<EntityUid, EntityUid> _avatarByBody = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AvatarConnectionComponent, ComponentShutdown>(OnAvatarShutdown);

        SubscribeLocalEvent<BitrunningDiskCarrierComponent, EntInsertedIntoContainerMessage>(OnCarrierContainerChanged);
        SubscribeLocalEvent<BitrunningDiskCarrierComponent, EntRemovedFromContainerMessage>(OnCarrierContainerChanged);
        SubscribeLocalEvent<BitrunningAbilityDiskComponent, EntGotInsertedIntoContainerMessage>(OnDiskInsertedIntoContainer);
        SubscribeLocalEvent<BitrunningAbilityDiskComponent, EntGotRemovedFromContainerMessage>(OnDiskRemovedFromContainer);
        SubscribeLocalEvent<BitrunningAbilityDiskComponent, ExaminedEvent>(OnDiskExamined);
        SubscribeLocalEvent<BitrunningAbilityDiskComponent, UseInHandEvent>(OnDiskUseInHand);

        Subs.BuiEvents<BitrunningAbilityDiskComponent>(BitrunningDiskUiKey.Key,
            subs =>
        {
            subs.Event<BitrunningDiskSelectOptionMessage>(OnDiskOptionSelected);
        });

        SubscribeLocalEvent<BitrunningSpawnCheeseActionEvent>(OnSpawnCheeseAction);
        SubscribeLocalEvent<BitrunningLesserHealActionEvent>(OnLesserHealAction);
    }

    /// <summary>
    /// Links the avatar to its runner's body once every connection field is set (connect, reconnect, polymorph)
    /// and grants the abilities of the disks the body carries.
    /// </summary>
    public void RefreshAvatarEffects(EntityUid avatarUid)
    {
        if (!TryComp<AvatarConnectionComponent>(avatarUid, out var avatarConnection))
            return;

        UnregisterAvatar(avatarUid);
        if (avatarConnection.OriginalBody is { } bodyUid && !TerminatingOrDeleted(bodyUid))
        {
            _avatarByBody[bodyUid] = avatarUid;
            EnsureComp<BitrunningDiskCarrierComponent>(bodyUid).Avatar = avatarUid;
        }

        UpdateAvatarEffects((avatarUid, avatarConnection));
    }

    /// <summary>
    /// Drops the body link of an avatar whose connection ended; disks moved afterwards no longer reach it.
    /// </summary>
    public void UnregisterAvatar(EntityUid avatarUid)
    {
        foreach (var (bodyUid, linkedAvatar) in _avatarByBody.ToArray())
        {
            if (linkedAvatar != avatarUid)
                continue;

            _avatarByBody.Remove(bodyUid);
            if (TryComp<BitrunningDiskCarrierComponent>(bodyUid, out var carrier) && carrier.Avatar == avatarUid)
                RemComp(bodyUid, carrier);
        }
    }

    private void OnAvatarShutdown(Entity<AvatarConnectionComponent> ent, ref ComponentShutdown args)
    {
        UnregisterAvatar(ent.Owner);
    }

    private void OnCarrierContainerChanged<T>(Entity<BitrunningDiskCarrierComponent> ent, ref T args)
    {
        // A container leaving or entering the body (backpack, belt) can carry disks with it.
        if (TryComp<AvatarConnectionComponent>(ent.Comp.Avatar, out var avatarComp))
            UpdateAvatarEffects((ent.Comp.Avatar, avatarComp));
    }

    private void OnDiskInsertedIntoContainer(Entity<BitrunningAbilityDiskComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (TryFindAvatarOwner(ent.Owner, out var avatarUid, out var avatarComp))
            UpdateAvatarEffects((avatarUid, avatarComp));
    }

    private void OnDiskRemovedFromContainer(Entity<BitrunningAbilityDiskComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        // The disk already sits in its new place, so look up the owner from the container it left.
        if (TryFindAvatarOwner(args.Container.Owner, out var avatarUid, out var avatarComp))
            UpdateAvatarEffects((avatarUid, avatarComp));
    }

    private void OnDiskUseInHand(Entity<BitrunningAbilityDiskComponent> ent, ref UseInHandEvent args)
    {
        args.Handled = true;

        if (ent.Comp.SelectedOption != null)
        {
            _popup.PopupEntity(Loc.GetString("bitrunning-disk-popup-already-selected", ("option", LocalizeOption(ent.Comp.SelectedOption))), ent, args.User, PopupType.SmallCaution);
            return;
        }

        _ui.TryOpenUi(ent.Owner, BitrunningDiskUiKey.Key, args.User);
        _ui.SetUiState(ent.Owner, BitrunningDiskUiKey.Key, new BitrunningDiskBoundUiState(ent.Comp.Options.Keys.ToList(), null));
    }

    private void OnDiskOptionSelected(Entity<BitrunningAbilityDiskComponent> ent, ref BitrunningDiskSelectOptionMessage args)
    {
        var user = args.Actor;
        if (ent.Comp.SelectedOption != null || !ent.Comp.Options.ContainsKey(args.Option))
            return;

        var foundAvatar = TryFindAvatarOwner(ent.Owner, out var avatarUid, out var avatarComp);
        if (foundAvatar && !IsDiskModificationAllowed(avatarComp))
        {
            _popup.PopupEntity(Loc.GetString("bitrunning-disk-popup-modifications-blocked"), ent, user, PopupType.SmallCaution);
            return;
        }

        ent.Comp.SelectedOption = args.Option;
        Dirty(ent);

        _popup.PopupEntity(Loc.GetString("bitrunning-disk-popup-selected", ("option", LocalizeOption(args.Option))), ent, user, PopupType.Medium);
        _ui.SetUiState(ent.Owner, BitrunningDiskUiKey.Key, new BitrunningDiskBoundUiState(ent.Comp.Options.Keys.ToList(), ent.Comp.SelectedOption));

        if (foundAvatar)
            UpdateAvatarEffects((avatarUid, avatarComp));
    }

    private void OnDiskExamined(Entity<BitrunningAbilityDiskComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (ent.Comp.SelectedOption == null)
            args.PushMarkup(Loc.GetString("bitrunning-disk-examine-unselected"));
        else
        {
            args.PushMarkup(Loc.GetString("bitrunning-disk-examine-selected",
                ("option", LocalizeOption(ent.Comp.SelectedOption))));
        }
    }

    private void UpdateAvatarEffects(Entity<AvatarConnectionComponent> avatar)
    {
        var holder = EnsureComp<BitrunningAvatarAbilityHolderComponent>(avatar);

        if (!IsDiskModificationAllowed(avatar.Comp) || avatar.Comp.OriginalBody is not { } bitrunnerUid)
        {
            RemoveAllGrantedActions(holder);
            return;
        }

        FindSelectedDisks(bitrunnerUid, out var selectedActionDisks, out var selectedItemDisks);

        foreach (var (diskUid, actionUid) in holder.ActionsByDisk.ToArray())
        {
            if (selectedActionDisks.ContainsKey(diskUid))
                continue;

            _actions.RemoveAction(actionUid);
            holder.ActionsByDisk.Remove(diskUid);
        }

        foreach (var (diskUid, actionProto) in selectedActionDisks)
        {
            if (holder.ActionsByDisk.ContainsKey(diskUid))
                continue;

            EntityUid? actionUid = null;
            _actions.AddAction(avatar.Owner, ref actionUid, actionProto, avatar.Owner);
            holder.ActionsByDisk[diskUid] = actionUid;
        }

        TryGrantDomainItems((avatar.Owner, avatar.Comp), selectedItemDisks);
    }

    private void RemoveAllGrantedActions(BitrunningAvatarAbilityHolderComponent holder)
    {
        foreach (var actionUid in holder.ActionsByDisk.Values)
        {
            _actions.RemoveAction(actionUid);
        }

        holder.ActionsByDisk.Clear();
    }

    private void FindSelectedDisks(EntityUid bitrunnerUid, out Dictionary<EntityUid, EntProtoId> actionDisks, out Dictionary<EntityUid, EntProtoId> itemDisks)
    {
        actionDisks = new Dictionary<EntityUid, EntProtoId>();
        itemDisks = new Dictionary<EntityUid, EntProtoId>();
        var visited = new HashSet<EntityUid>();
        var queue = new Queue<EntityUid>();
        queue.Enqueue(bitrunnerUid);

        while (queue.TryDequeue(out var current))
        {
            if (!visited.Add(current))
                continue;

            if (TryComp<BitrunningAbilityDiskComponent>(current, out var disk) && disk.SelectedOption is { } selected && disk.Options.TryGetValue(selected, out var prototype))
            {
                switch (disk.GrantMode)
                {
                    case BitrunningDiskGrantMode.Action:
                        actionDisks[current] = prototype;
                        break;
                    case BitrunningDiskGrantMode.Item:
                        itemDisks[current] = prototype;
                        break;
                }
            }

            if (!TryComp<ContainerManagerComponent>(current, out var manager))
                continue;

            foreach (var container in manager.Containers.Values)
            {
                foreach (var contained in container.ContainedEntities)
                {
                    queue.Enqueue(contained);
                }
            }
        }
    }

    private void TryGrantDomainItems(Entity<AvatarConnectionComponent> avatar, Dictionary<EntityUid, EntProtoId> selectedItemDisks)
    {
        if (avatar.Comp.Server is not { } serverUid || !TryComp<QuantumServerComponent>(serverUid, out var server))
            return;

        foreach (var (diskUid, itemProto) in selectedItemDisks)
        {
            if (server.GrantedItemDisks.Contains(diskUid))
                continue;

            var spawned = Spawn(itemProto, Transform(avatar.Owner).Coordinates);
            TryInsertIntoAvatarInventory(avatar.Owner, spawned);

            server.GrantedItemDisks.Add(diskUid);
        }
    }

    private void TryInsertIntoAvatarInventory(EntityUid avatarUid, EntityUid itemUid)
    {
        if (TryEquipOnAvatar(avatarUid, itemUid))
            return;

        if (_inventory.TryGetSlotEntity(avatarUid, "back", out var backUid) && TryComp<StorageComponent>(backUid.Value, out var storage) && _storage.Insert(backUid.Value, itemUid, out _, storageComp: storage, playSound: false))
            return;

        _hands.TryPickupAnyHand(avatarUid, itemUid, checkActionBlocker: false);
    }

    private bool TryEquipOnAvatar(EntityUid avatarUid, EntityUid itemUid)
    {
        if (!TryComp<ClothingComponent>(itemUid, out var clothing))
            return false;

        if (!_inventory.TryGetSlots(avatarUid, out var slots))
            return false;

        foreach (var slot in slots)
        {
            if ((slot.SlotFlags & clothing.Slots) == SlotFlags.NONE)
                continue;

            if (_inventory.TryEquip(avatarUid, itemUid, slot.Name, silent: true))
                return true;
        }

        return false;
    }

    private bool IsDiskModificationAllowed(AvatarConnectionComponent avatarConnection)
    {
        if (avatarConnection.Server is not { } serverUid || !TryComp<QuantumServerComponent>(serverUid, out var server))
            return true;

        return server.AllowDiskModifications;
    }

    private bool TryFindAvatarOwner(EntityUid entity, out EntityUid avatarUid, out AvatarConnectionComponent avatarComp)
    {
        avatarUid = default;
        avatarComp = default!;

        var ancestors = new HashSet<EntityUid>();
        var current = entity;
        while (TryComp(current, out TransformComponent? xform))
        {
            ancestors.Add(current);

            var parent = xform.ParentUid;
            if (parent == EntityUid.Invalid || parent == current)
                break;

            current = parent;
        }

        foreach (var ancestor in ancestors)
        {
            if (!TryFindAvatarByOriginalBody(ancestor, out avatarUid, out avatarComp))
                continue;

            return true;
        }

        avatarUid = default;
        avatarComp = default!;
        return false;
    }

    private bool TryFindAvatarByOriginalBody(EntityUid bodyUid, out EntityUid avatarUid, out AvatarConnectionComponent avatarComp)
    {
        avatarUid = default;
        avatarComp = default!;

        if (!_avatarByBody.TryGetValue(bodyUid, out var foundUid))
            return false;

        if (!TryComp(foundUid, out AvatarConnectionComponent? foundComp))
        {
            _avatarByBody.Remove(bodyUid);
            return false;
        }

        avatarComp = foundComp;
        avatarUid = foundUid;
        return true;
    }

    private void OnSpawnCheeseAction(BitrunningSpawnCheeseActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var origin = Transform(args.Performer).Coordinates;
        var radius = Math.Clamp(args.Radius, 0, 8);
        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                var spawnCoords = new EntityCoordinates(origin.EntityId, origin.Position + new Vector2(x, y));
                Spawn(args.PrototypeId, spawnCoords);
            }
        }
    }

    private void OnLesserHealAction(BitrunningLesserHealActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var heal = new DamageSpecifier();
        heal.DamageDict["Blunt"] = -20f;
        heal.DamageDict["Heat"] = -20f;
        _damageable.TryChangeDamage(args.Performer, heal, ignoreResistances: true);
    }

    private string LocalizeOption(string optionKey)
    {
        return _loc.TryGetString(optionKey, out var localizedOption)
            ? localizedOption
            : optionKey;
    }
}
