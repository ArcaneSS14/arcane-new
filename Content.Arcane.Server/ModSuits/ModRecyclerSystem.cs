// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Arcane.Shared.ModSuits;
using Content.Arcane.Shared.ModSuits.Components;
using Content.Arcane.Shared.ModSuits.Systems;
using Content.Server.Materials;
using Content.Shared.Actions;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Materials;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.ModSuits;

public sealed partial class ModRecyclerSystem : VirtualController
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private MaterialStorageSystem _materials = default!;
    [Dependency] private ModSuitSystem _suits = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private const int MaxProductsPerUnload = 30;
    private readonly HashSet<Entity<ItemComponent>> _items = new();

    [SubscribeLocalEvent]
    private void OnWorn(Entity<ModRecyclerModuleComponent> module, ref ModModuleWornEvent args)
    {
        if (_actions.AddAction(args.Wearer, ref module.Comp.UnloadActionEntity, module.Comp.UnloadAction, module))
            _actions.SetEntityIcon(module.Comp.UnloadActionEntity.Value, module);
    }

    [SubscribeLocalEvent]
    private void OnUnload(Entity<ModRecyclerModuleComponent> module, ref ModRecyclerUnloadActionEvent args)
    {
        if (args.Handled || !_suits.TryGetInstalledModuleSuit(module, out var suit)
            || suit.Comp.Wearer != args.Performer || _containers.IsEntityInContainer(args.Performer))
            return;

        args.Handled = true;
        var coords = Transform(args.Performer).Coordinates;
        if (module.Comp.Product is { } product && module.Comp.Products > 0)
        {
            var count = Math.Min(module.Comp.Products, MaxProductsPerUnload);
            for (var i = 0; i < count; i++)
                Spawn(product, coords);
            module.Comp.Products -= count;
        }
        else if (module.Comp.Materials.Count > 0)
        {
            Dictionary<ProtoId<MaterialPrototype>, int> remainder = new();
            foreach (var (material, amount) in module.Comp.Materials)
            {
                _materials.SpawnMultipleFromMaterial(amount, material.Id, coords, out var overflow);
                if (overflow > 0)
                    remainder[material] = overflow;
            }

            module.Comp.Materials = remainder;
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("modsuit-recycler-empty"), args.Performer, args.Performer);
        }
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ModRecyclerModuleComponent, ModModuleComponent>();
        while (query.MoveNext(out var uid, out var recycler, out var module))
        {
            if (!module.Active || !_suits.TryGetInstalledModuleSuit(uid, out var suit)
                || !_suits.IsPartDeployed(suit, "outerClothing")
                || suit.Comp.Wearer is not { } wearer || _containers.IsEntityInContainer(wearer))
                continue;

            var collect = now >= recycler.NextCollect;
            if (collect)
                recycler.NextCollect = now + recycler.Interval;
            Pull((uid, recycler), wearer, collect);
        }
    }

    private void Pull(Entity<ModRecyclerModuleComponent> module, EntityUid wearer, bool collect)
    {
        var origin = TransformSystem.GetMapCoordinates(wearer);
        if (origin.MapId == MapId.Nullspace)
            return;

        var collected = false;
        _items.Clear();
        _lookup.GetEntitiesInRange(origin, module.Comp.Range, _items, LookupFlags.Dynamic | LookupFlags.Sundries);
        foreach (var item in _items)
        {
            if (TerminatingOrDeleted(item) || EntityManager.IsQueuedForDeletion(item)
                || _containers.IsEntityInContainer(item) || Transform(item).Anchored
                || HasComp<MobStateComponent>(item) || !_whitelist.IsWhitelistPass(module.Comp.Whitelist, item)
                || ContainsItems(item) || !_interaction.InRangeUnobstructed(wearer, item.Owner, range: module.Comp.Range))
                continue;

            var xform = Transform(item);
            var offset = origin.Position - TransformSystem.GetMapCoordinates(xform).Position;
            var distance = offset.Length();
            if (TryComp<PhysicsComponent>(item, out var body) && body.BodyType == BodyType.Dynamic)
            {
                var parentRotation = TransformSystem.GetWorldRotation(xform) - xform.LocalRotation;
                var velocity = distance > module.Comp.CatchDistance
                    ? (-parentRotation).RotateVec(offset / distance * module.Comp.PullSpeed)
                    : Vector2.Zero;
                PhysicsSystem.SetLinearVelocity(item, velocity, body: body);
            }

            if (!collect || distance > module.Comp.CatchDistance)
                continue;

            if (module.Comp.Product != null)
                module.Comp.Products++;
            else if (TryComp<PhysicalCompositionComponent>(item, out var composition))
            {
                foreach (var (material, amount) in composition.MaterialComposition)
                    module.Comp.Materials[material] = module.Comp.Materials.GetValueOrDefault(material) + amount;
            }

            QueueDel(item);
            collected = true;
        }

        if (collected)
            _audio.PlayPvs(module.Comp.Sound, wearer);
    }

    private bool ContainsItems(EntityUid item)
    {
        TryComp<SolutionManagerComponent>(item, out var solutions);
        foreach (var container in _containers.GetAllContainers(item))
        {
            if (container.ID != solutions?.Container && container.ContainedEntities.Count > 0)
                return true;
        }

        return false;
    }
}
