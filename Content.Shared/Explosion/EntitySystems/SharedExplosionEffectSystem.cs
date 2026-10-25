// Arcane-Start
using Content.Shared.Explosion.Components;
using Content.Shared.Throwing;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.Explosion.EntitySystems;

public abstract class SharedExplosionEffectSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ExplosionEffectComponent, ExplosiveTriggeredEvent>(OnExplosiveTriggered);
    }

    private void OnExplosiveTriggered(Entity<ExplosionEffectComponent> ent, ref ExplosiveTriggeredEvent args)
    {
        // The trigger is raised by the server ExplosionSystem. Keep the guard here as
        // well so replayed or predicted events can never duplicate effect entities.
        if (!_net.IsServer || TerminatingOrDeleted(ent))
            return;

        var coords = _transform.GetMapCoordinates(ent);
        if (coords.MapId == MapId.Nullspace)
            return;

        foreach (var effect in ent.Comp.VisualEffects)
        {
            Spawn(effect, coords);
        }

        if (ent.Comp.MaxShrapnel <= 0 || ent.Comp.ShrapnelEffects.Count == 0)
            return;

        var min = Math.Max(0, ent.Comp.MinShrapnel);
        var max = Math.Max(min, ent.Comp.MaxShrapnel);
        foreach (var effect in ent.Comp.ShrapnelEffects)
        {
            var count = _random.Next(min, max + 1);
            for (var i = 0; i < count; i++)
            {
                var shrapnel = Spawn(effect, coords);
                if (TerminatingOrDeleted(shrapnel))
                    continue;

                _throwing.TryThrow(shrapnel, _random.NextAngle().ToVec().Normalized(), ent.Comp.ShrapnelSpeed / 10f);
            }
        }
    }
}
// Arcane-End
