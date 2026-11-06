// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mobs.Systems;
using Content.Trauma.Shared.Knowledge.Components;

namespace Content.Trauma.Shared.Knowledge.Systems;

public sealed partial class ExperienceOnDamageSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExperienceOnDamageComponent, TookDamageEvent>(OnTookDamage);
    }

    private void OnTookDamage(Entity<ExperienceOnDamageComponent> ent, ref TookDamageEvent args)
    {
        if (!_mob.IsAlive(args.Target))
            return;

        var xp = Math.Min(args.Damage / ent.Comp.DamageScale, ent.Comp.MaxGain);
        _knowledge.AddExperience(ent.Owner, args.Target, xp); // Arcane-Edit: limit by damage in one hit > no limit
    }
}
