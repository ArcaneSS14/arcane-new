using Content.Arcane.Common.Medical.Skills;
using Content.Goobstation.Common.Chemistry;
using Content.Medical.Shared.Surgery;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Systems;
using Content.Trauma.Shared.Weapons.Classes;

namespace Content.Arcane.Shared.Knowledge;

/// <summary>
/// Gives experience for practicing a skill: fighting with weapons and treating patients.
/// </summary>
public sealed partial class PracticeExperienceSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;
    [Dependency] private WeaponClassSystem _weaponClass = default!;

    private static readonly EntProtoId FirstAidKnowledge = "FirstAidKnowledge";
    private static readonly EntProtoId SurgeryKnowledge = "SurgeryKnowledge";

    private const int WeaponExperience = 1;
    private const int FirstAidExperience = 2;
    private const int SurgeryExperience = 4;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(MeleeHitEvent args)
    {
        if (!args.IsHit || args.Handled || !HitLivingTarget(args))
            return;

        var skill = args.Weapon == args.User
            ? SharedMeleeWeaponSystem.MeleeKnowledge
            : _weaponClass.GetKnowledge(args.Weapon);

        if (skill is { } id)
            AddExperience(args.User, id, WeaponExperience);
    }

    [SubscribeLocalEvent]
    private void OnGunShot(Entity<WeaponClassComponent> ent, ref GunShotEvent args)
    {
        if (args.User != ent.Owner && _weaponClass.GetKnowledge(ent) is { } skill)
            AddExperience(args.User, skill, WeaponExperience);
    }

    [SubscribeLocalEvent]
    private void OnInjected(Entity<InjectorComponent> ent, ref AfterInjectedEvent args)
    {
        // injectors also fill beakers, that's not first aid
        if (HasComp<BloodstreamComponent>(args.Target))
            AddExperience(args.User, FirstAidKnowledge, FirstAidExperience);
    }

    [SubscribeLocalEvent]
    private void OnAppliedHealing(Entity<KnowledgeHolderComponent> ent, ref UserAppliedHealingEvent args)
        => AddExperience(ent, FirstAidKnowledge, FirstAidExperience);

    [SubscribeLocalEvent]
    private void OnSurgeryStep(Entity<KnowledgeHolderComponent> ent, ref SurgeryStepEvent args)
        => AddExperience(ent, SurgeryKnowledge, SurgeryExperience);

    private bool HitLivingTarget(MeleeHitEvent args)
    {
        foreach (var target in args.HitEntities)
        {
            if (target != args.User && _mobState.IsAlive(target))
                return true;
        }

        return false;
    }

    private void AddExperience(EntityUid user, EntProtoId skill, int experience)
    {
        if (_knowledge.GetContainer(user) is { } brain)
            _knowledge.AddExperience(brain, skill, experience);
    }
}
