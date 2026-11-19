using Content.Shared.EntityConditions;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.Achievements.Conditions;

public sealed partial class HugCondition : AchievementCondition
{
    [DataField] public bool CreditTarget;
    [DataField] public bool SameName;
    [DataField] public EntityCondition[]? HuggerRequirements;
}

public sealed partial class HugConditionSystem : AchievementConditionSystem<HugCondition>
{
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HumanoidProfileComponent, InteractionSuccessEvent>(OnHugged);
        SubscribeLocalEvent<HumanoidProfileComponent, GhostHugSuccessEvent>(OnGhostHugged);
    }

    private void OnHugged(Entity<HumanoidProfileComponent> ent, ref InteractionSuccessEvent args) => ProgressHug(ent, args.User);

    private void OnGhostHugged(Entity<HumanoidProfileComponent> ent, ref GhostHugSuccessEvent args) => ProgressHug(ent, args.User);

    private void ProgressHug(Entity<HumanoidProfileComponent> ent, EntityUid user)
    {
        if (user == ent.Owner)
            return;
        foreach (var (proto, condition) in Entries)
        {
            if (condition.SameName && Name(user) != Name(ent)
                || condition.HuggerRequirements is { } requirements && !_conditions.TryConditions(user, requirements))
                continue;
            Achievements.AddProgress(condition.CreditTarget ? ent : user, proto);
        }
    }
}

public sealed partial class HarmBatonCondition : AchievementCondition;

public sealed partial class HarmBatonConditionSystem : AchievementConditionSystem<HarmBatonCondition>
{
    [Dependency] private ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HumanoidProfileComponent, AttackedEvent>(OnAttacked);
    }

    private void OnAttacked(Entity<HumanoidProfileComponent> ent, ref AttackedEvent args)
    {
        if (args.User == ent.Owner || _toggle.IsActivated(args.Used) || Prototype(args.Used) is not { } weapon)
            return;
        // Trauma replaced StunbatonComponent. These are its two source weapon families;
        // the Stunbaton tag alone also includes unrelated knuckle dusters here.
        foreach (var (id, _) in ProtoMan.EnumerateAllParents<EntityPrototype>(weapon.ID, includeSelf: true))
        {
            if (id is not ("Stunbaton" or "Stunprod"))
                continue;
            ProgressAll(args.User);
            return;
        }
    }
}

public sealed partial class UnprotectedWeldingCondition : AchievementCondition;

public sealed partial class UnprotectedWeldingConditionSystem : AchievementConditionSystem<UnprotectedWeldingCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BlindableComponent, UnprotectedToolEyeDamageEvent>(OnBurned);
    }

    private void OnBurned(Entity<BlindableComponent> ent, ref UnprotectedToolEyeDamageEvent args) => ProgressAll(ent);
}
