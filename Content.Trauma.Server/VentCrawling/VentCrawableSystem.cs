// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Zombies; // Arcane
using Content.Trauma.Shared.VentCrawling;
using Content.Trauma.Shared.VentCrawling.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Containers;
using Content.Trauma.Common.VentCrawling;

namespace Content.Trauma.Server.VentCrawling;

public sealed partial class VentCrawableSystem : EntitySystem
{
    [Dependency] private SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private SharedContainerSystem _containerSystem = default!;
    [Dependency] private SharedTransformSystem _xformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VentCrawlerHolderComponent, VentCrawlingExitEvent>(OnVentCrawlingExitEvent);
        SubscribeLocalEvent<BeingVentCrawlerComponent, EntityZombifiedEvent>(OnZombified); // Arcane
    }

    // Arcane-Start
    private void OnZombified(Entity<BeingVentCrawlerComponent> ent, ref EntityZombifiedEvent args)
    {
        var holder = ent.Comp.Holder;
        if (!TryComp<VentCrawlerHolderComponent>(holder, out var crawler) ||
            crawler.IsExitingVentCraws ||
            !crawler.Container.Contains(ent))
            return;

        var exit = new VentCrawlingExitEvent { holderTransform = Transform(holder) };
        RaiseLocalEvent(holder, ref exit);
    }
    // Arcane-End

    /// <summary>
    /// Exits the vent craws for the specified VentCrawlerHolderComponent, removing it and any contained entities from the craws.
    /// </summary>
    /// <param name="uid">The EntityUid of the VentCrawlerHolderComponent.</param>
    /// <param name="holder">The VentCrawlerHolderComponent instance.</param>
    /// <param name="holderTransform">The TransformComponent instance for the VentCrawlerHolderComponent.</param>
    private void OnVentCrawlingExitEvent(EntityUid uid, VentCrawlerHolderComponent holder, ref VentCrawlingExitEvent args)
    {
        var holderTransform = args.holderTransform;

        if (Terminating(uid))
            return;

        if (!Resolve(uid, ref holderTransform))
            return;

        if (holder.IsExitingVentCraws)
        {
            Log.Error("Tried exiting VentCraws twice. This should never happen.");
            return;
        }

        holder.IsExitingVentCraws = true;

        foreach (var entity in holder.Container.ContainedEntities.ToArray())
        {
            RemComp<BeingVentCrawlerComponent>(entity);

            var meta = MetaData(entity);
            _containerSystem.Remove(entity, holder.Container, reparent: false, force: true);

            var xform = Transform(entity);
            if (xform.ParentUid != uid)
                continue;

            _xformSystem.AttachToGridOrMap(entity, xform);

            if (TryComp<VentCrawlerComponent>(entity, out var ventCrawComp))
            {
                ventCrawComp.InTube = false;
                Dirty(entity , ventCrawComp);
            }

            if (TryComp(entity, out PhysicsComponent? physics))
                _physicsSystem.WakeBody(entity, body: physics);
        }

        Del(uid);
    }
}
