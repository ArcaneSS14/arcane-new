using System.Linq;
using Content.Arcane.Common.ModSuits;
using Content.Arcane.Shared.CloneProjector;
using Content.Arcane.Shared.ModSuits.Components;
using Content.Arcane.Shared.ModSuits.Systems;
using Content.Server.Construction.Components;
using Content.Shared.Access.Components;
using Content.Shared.Actions.Components;
using Content.Shared.Buckle;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Clothing.Components;
using Content.Shared.Destructible;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.PDA;
using Content.Trauma.Common.Carrying;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.CloneProjector;

public sealed partial class CloneProjectorSystem
{
    private const string IdSlot = "id";
    private static readonly EntProtoId ProjectedPdaCard = "PassengerIDCard";

    [Dependency] private CommonCarryingSystem _carrying = default!;
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private SharedJointSystem _joints = default!;
    [Dependency] private SharedMechSystem _mechs = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ModSuitSystem _modsuits = default!;

    private void CopyEquipment(Entity<CloneProjectorComponent> projector, EntityUid host, Entity<GeminiProjectionComponent> clone)
    {
        HashSet<EntityUid> eligibleItems = new();
        HashSet<EntityUid> equippedCopies = new();
        List<(Entity<ModSuitComponent> Source, EntityUid Copy)> projectedSuits = new();
        List<(EntityUid Source, string Slot)> suitStorage = new();
        List<(EntityUid Source, EntityUid Copy)> identities = new();
        var slots = _inventory.GetSlotEnumerator(host);
        while (slots.NextItem(out var item, out var slot))
        {
            if (_whitelist.CheckBoth(item, projector.Comp.ClonedItemBlacklist, projector.Comp.ClonedItemWhitelist))
                eligibleItems.Add(item);

            var resolve = new ResolveEquipmentCopyItemEvent(item);
            RaiseLocalEvent(item, ref resolve);
            if (resolve.Item is not { } source)
                continue;

            if (slot.SlotFlags.HasFlag(SlotFlags.SUITSTORAGE))
                suitStorage.Add((source, slot.Name));
            else
                CopySlot(source, slot.Name);
        }

        foreach (var (source, copy) in projectedSuits)
            _modsuits.CopyDeployment(source, copy, eligibleItems, equippedCopies);

        foreach (var (source, slot) in suitStorage)
            CopySlot(source, slot);

        // Lock the copies after deployment has moved the parts and their underlying clothing.
        foreach (var copy in equippedCopies)
            TrackCopy(copy, clone.Comp.Equipment);

        foreach (var (source, copy) in identities)
            CopyJobTitle(source, copy, clone.Comp.Equipment);

        void CopySlot(EntityUid item, string slot)
        {
            if (HasComp<AttachedClothingComponent>(item)
                || HasComp<GeminiEquipmentComponent>(item)
                || _whitelist.IsWhitelistFail(projector.Comp.ClonedItemWhitelist, item)
                || _whitelist.IsWhitelistPass(projector.Comp.ClonedItemBlacklist, item)
                || Prototype(item) is not { } prototype)
                return;

            var copy = Spawn(prototype.ID, Transform(host).Coordinates);
            if (TryComp<ModSuitComponent>(copy, out var projection))
                _modsuits.PrepareProjectedSuit((copy, projection));

            if (!_inventory.TryEquip(clone, copy, slot, silent: true, force: true))
            {
                Del(copy);
                return;
            }

            equippedCopies.Add(copy);
            if (projection != null && TryComp<ModSuitComponent>(item, out var original))
                projectedSuits.Add(((item, original), copy));

            if (slot == IdSlot)
                identities.Add((item, copy));
        }
    }

    private void CopyJobTitle(EntityUid original, EntityUid copy, HashSet<EntityUid> copies)
    {
        // Old Arcane #315: preserve the job title from the equipment being projected.
        if (!_idCards.TryGetIdCard(original, out var originalCard)
            || originalCard.Comp.LocalizedJobTitle is not { Length: > 0 } title)
            return;

        if (_idCards.TryGetIdCard(copy, out var copiedCard))
        {
            _idCards.TryChangeJobTitle(copiedCard, title, copiedCard.Comp);
            return;
        }

        if (!TryComp(copy, out PdaComponent? pda) || pda.IdSlot.ContainerSlot is not { } slot)
            return;

        var card = Spawn(ProjectedPdaCard, Transform(copy).Coordinates);
        TrackCopy(card, copies);
        _idCards.TryChangeJobTitle(card, title);
        if (!_containers.Insert(card, slot))
        {
            copies.Remove(card);
            Del(card);
            return;
        }
    }

    private void TrackCopy(EntityUid copy, HashSet<EntityUid> copies)
    {
        if (!copies.Add(copy))
            return;

        RemComp<AccessComponent>(copy);
        if (!HasComp<AttachedClothingComponent>(copy))
            EnsureComp<UnremoveableComponent>(copy);
        RemComp<DestructibleComponent>(copy);
        RemComp<ConstructionComponent>(copy);
        EnsureComp<GeminiEquipmentComponent>(copy);

        if (TryComp<ModSuitComponent>(copy, out var suit))
        {
            foreach (var part in suit.PartEntities.Values)
                TrackCopy(part, copies);
        }
        else if (TryComp<ModPartComponent>(copy, out var modPart) && modPart.StoredClothing?.ContainedEntity is { } layer)
        {
            TrackCopy(layer, copies);
        }

        foreach (var (_, solution) in _solutions.EnumerateSolutions(copy))
            _solutions.RemoveAllSolution(solution);

        if (!TryComp(copy, out ContainerManagerComponent? manager))
            return;

        // Keep attached clothing, MOD parts and action entities. Clear other generated fills.
        foreach (var container in _containers.GetAllContainers(copy, manager))
        {
            foreach (var content in container.ContainedEntities.ToArray())
            {
                if (HasComp<AttachedClothingComponent>(content) || HasComp<ModPartComponent>(content))
                    TrackCopy(content, copies);
                else if (!copies.Contains(content) && !HasComp<ActionComponent>(content) && !HasComp<SolutionComponent>(content))
                    Del(content);
            }
        }
    }

    private void ReleaseEquipment(Entity<GeminiProjectionComponent> clone, bool destroyCopies)
    {
        if (TryComp(clone, out CarryingComponent? carrying))
            _carrying.DropCarried(clone, carrying.Carried);
        if (!TerminatingOrDeleted(clone))
        {
            if (TryComp(clone, out MechPilotComponent? pilot)
                && TryComp(pilot.Mech, out MechComponent? mech)
                && mech.PilotSlot?.ContainedEntity == clone.Owner)
                _mechs.TryEject(pilot.Mech, mech, clone.Owner);

            _buckle.Unbuckle(clone.Owner, null);
        }
        _joints.RecursiveClearJoints(clone);

        if (_transform.TryGetMapOrGridCoordinates(clone, out var clonePosition))
            ReleaseRealItems(clone, clonePosition.Value);

        foreach (var copy in clone.Comp.Equipment)
        {
            if (TerminatingOrDeleted(copy))
                continue;

            if (_transform.TryGetMapOrGridCoordinates(copy, out var position))
                ReleaseRealItems(copy, position.Value);

            if (destroyCopies)
                QueueDel(copy);
        }
    }

    private void ReleaseRealItems(EntityUid owner, EntityCoordinates position)
    {
        if (!TryComp(owner, out ContainerManagerComponent? manager))
            return;

        foreach (var container in _containers.GetAllContainers(owner, manager))
        {
            foreach (var item in container.ContainedEntities.ToArray())
            {
                if (!HasComp<ItemComponent>(item) || HasComp<GeminiEquipmentComponent>(item)
                    || HasComp<VirtualItemComponent>(item) || HasComp<GeminiOrganComponent>(item))
                    continue;

                _containers.Remove(item, container, force: true, destination: position);
            }
        }
    }

    private void OnCopyUnequipAttempt(Entity<GeminiEquipmentComponent> ent, ref BeingUnequippedAttemptEvent args)
    {
        if (!HasComp<AttachedClothingComponent>(ent))
            args.Cancel();
    }

    private void OnCopyTerminating(Entity<GeminiEquipmentComponent> ent, ref EntityTerminatingEvent args)
    {
        if (_transform.TryGetMapOrGridCoordinates(ent, out var position))
            ReleaseRealItems(ent, position.Value);
    }
}
