using Content.Arcane.Shared.Explosion.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;

namespace Content.Arcane.Shared.Explosion.Systems;

public sealed partial class ExplosionProjectileCollisionSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;

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
