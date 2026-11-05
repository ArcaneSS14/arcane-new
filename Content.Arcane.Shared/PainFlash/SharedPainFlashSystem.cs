using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Timing;

namespace Content.Arcane.Shared.PainFlash;

public abstract partial class SharedPainFlashSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PainFlashComponent, DamageChangedEvent>(OnDamageChanged);
    }

    protected abstract void OnDamageChanged(Entity<PainFlashComponent> ent, ref DamageChangedEvent args);

    protected static bool TryGetPain(DamageChangedEvent args, out FixedPoint2 damage)
    {
        damage = FixedPoint2.Zero;

        if (!args.DamageIncreased || args.DamageDelta is null)
            return false;

        foreach (var value in args.DamageDelta.DamageDict.Values)
        {
            if (value > 0)
                damage += value;
        }

        return damage > 0;
    }
}
