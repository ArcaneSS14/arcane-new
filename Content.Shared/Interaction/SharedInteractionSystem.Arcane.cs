// Arcane-Start
using Content.Shared.Inventory.VirtualItem;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared.Interaction;

public abstract partial class SharedInteractionSystem
{
    [Dependency] private INetManager _arcaneNet = default!;

    public void DoContactInteraction(
        EntityUid performer,
        EntityUid? target,
        EntityUid? used,
        bool predicted,
        HandledEntityEventArgs? args = null,
        bool interactionParticles = true,
        ArcaneInteractionParticleType particleType = ArcaneInteractionParticleType.Use)
    {
        DoContactInteraction(performer, target, args);

        if (!interactionParticles || target is not { } targetUid || args?.Handled == false || performer == targetUid)
            return;

        if (!TryComp(performer, out MetaDataComponent? performerMeta) || performerMeta.EntityPaused
            || !TryComp(targetUid, out MetaDataComponent? targetMeta) || targetMeta.EntityPaused
            || HasComp<VirtualItemComponent>(targetUid)
            || performerMeta.CreationTick == _gameTiming.CurTick
            || targetMeta.CreationTick == _gameTiming.CurTick)
            return;

        if (_arcaneNet.IsServer)
        {
            var filter = predicted
                ? Filter.PvsExcept(performer, entityManager: EntityManager)
                : Filter.Pvs(performer, entityManager: EntityManager);

            RaiseNetworkEvent(new ArcaneInteractionParticleEvent(
                GetNetEntity(performer),
                GetNetEntity(used),
                GetNetEntity(targetUid),
                particleType), filter);
        }
        else if (_gameTiming.IsFirstTimePredicted)
        {
            RaiseLocalEvent(new ArcaneInteractionParticleEvent(
                GetNetEntity(performer),
                GetNetEntity(used),
                GetNetEntity(targetUid),
                particleType));
        }
    }
}
// Arcane-End
