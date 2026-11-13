using System.Linq;
using Content.Arcane.Shared.CloneProjector;
using Content.Server.Construction.Components;
using Content.Shared.Actions.Components;
using Content.Shared.Buckle;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Clothing.Components;
using Content.Shared.Destructible;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Trauma.Common.Carrying;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;

namespace Content.Arcane.Server.CloneProjector;

public sealed partial class CloneProjectorSystem
{
    [Dependency] private CommonCarryingSystem _carrying = default!;
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private SharedJointSystem _joints = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private void CopyEquipment(Entity<CloneProjectorComponent> projector, EntityUid host, Entity<GeminiProjectionComponent> clone)
    {
        var slots = _inventory.GetSlotEnumerator(host);
        while (slots.NextItem(out var item, out var slot))
        {
            if (HasComp<AttachedClothingComponent>(item)
                || _whitelist.IsWhitelistFail(projector.Comp.ClonedItemWhitelist, item)
                || _whitelist.IsWhitelistPass(projector.Comp.ClonedItemBlacklist, item)
                || Prototype(item) is not { } prototype)
                continue;

            var copy = Spawn(prototype.ID, Transform(host).Coordinates);
            if (!_inventory.TryEquip(clone, copy, slot.Name, silent: true, force: true))
            {
                Del(copy);
                continue;
            }

            TrackCopy(copy, clone.Comp.Equipment);
        }
    }

    private void TrackCopy(EntityUid copy, HashSet<EntityUid> copies)
    {
        if (!HasComp<AttachedClothingComponent>(copy))
            EnsureComp<UnremoveableComponent>(copy);
        RemComp<DestructibleComponent>(copy);
        RemComp<ConstructionComponent>(copy);
        EnsureComp<GeminiEquipmentComponent>(copy);
        copies.Add(copy);

        foreach (var (_, solution) in _solutions.EnumerateSolutions(copy))
            _solutions.RemoveAllSolution(solution);

        // Keep the suit's attached clothing and its actions. Discard generated items from fills.
        foreach (var container in _containers.GetAllContainers(copy))
        {
            foreach (var content in container.ContainedEntities.ToArray())
            {
                if (HasComp<AttachedClothingComponent>(content))
                    TrackCopy(content, copies);
                else if (!HasComp<ActionComponent>(content) && !HasComp<SolutionComponent>(content))
                    Del(content);
            }
        }
    }

    private void ReleaseEquipment(Entity<GeminiProjectionComponent> clone, bool destroyCopies)
    {
        if (TryComp(clone, out CarryingComponent? carrying))
            _carrying.DropCarried(clone, carrying.Carried);
        if (!TerminatingOrDeleted(clone))
            _buckle.Unbuckle(clone.Owner, null);
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
        foreach (var container in _containers.GetAllContainers(owner))
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
