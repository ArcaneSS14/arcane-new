// Arcane-Start
using Content.Shared.Explosion.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Explosion.EntitySystems;

public sealed class ExplosionProjectileCollisionSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeleteOnCollideComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(Entity<DeleteOnCollideComponent> ent, ref StartCollideEvent args)
    {
        if (!_net.IsServer || TerminatingOrDeleted(ent))
            return;

        QueueDel(ent);
    }
}
// Arcane-End
