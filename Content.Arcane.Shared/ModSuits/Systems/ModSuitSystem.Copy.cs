using Content.Arcane.Common.ModSuits;
using Content.Arcane.Shared.ModSuits.Components;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    private void InitializeEquipmentCopy()
    {
        SubscribeLocalEvent<ModPartComponent, ResolveEquipmentCopyItemEvent>(OnResolveEquipmentCopy);
        SubscribeLocalEvent<ModSuitComponent, EquipmentCopyEquippedEvent>(OnEquipmentCopied);
    }

    private void OnResolveEquipmentCopy(Entity<ModPartComponent> part, ref ResolveEquipmentCopyItemEvent args)
    {
        if (part.Comp.Suit != null)
            args.Item = part.Comp.StoredClothing?.ContainedEntity;
    }

    private void OnEquipmentCopied(Entity<ModSuitComponent> source, ref EquipmentCopyEquippedEvent args)
    {
        CopyDeployment(source, args.Copy, args.EligibleItems, args.EquippedCopies);
    }

    private void CopyDeployment(Entity<ModSuitComponent> source, EntityUid target, IReadOnlySet<EntityUid> eligibleItems,
        IReadOnlySet<EntityUid> equippedCopies)
    {
        if (_net.IsClient || !TryComp(target, out ModSuitComponent? control) || control.Wearer is not { } wearer
            || control.State != ModSuitState.Inactive)
            return;

        var suit = (target, control);
        foreach (var (slot, sourcePart) in source.Comp.PartEntities)
        {
            if (!eligibleItems.Contains(sourcePart) || !IsPartDeployed(source, slot)
                || _inventory.TryGetSlotEntity(wearer, slot, out var occupant) && !equippedCopies.Contains(occupant.Value))
                continue;

            TryDeployPart(suit, slot, wearer, copying: true);
        }

        if (source.Comp.State != ModSuitState.Active || !control.Powered || !AllPartsDeployed(suit))
            return;

        foreach (var part in control.PartEntities.Values)
        {
            if (TryComp(part, out ModPartComponent? modPart))
                SetPartSealed((part, modPart), true);
        }

        SetState(suit, ModSuitState.Active);
    }
}
