using Content.Medical.Common.EntityEffects;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityConditions;
using Content.Shared.EntityConditions.Conditions;
using Content.Shared.EntityConditions.Conditions.Body;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.EntityEffects.Effects.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Temperature.Components;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.Medical.Defibrillator;

/// <summary>
/// Finds oxygen medicines from their existing effects, without administering them or changing the patient's state.
/// </summary>
public sealed partial class OxygenMedicineSystem : EntitySystem
{
    public const int ShockDose = 4;

    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;

    private readonly Dictionary<ProtoId<ReagentPrototype>, OxygenEffect[]> _cache = new();
    private (EntityUid Patient, MobState State, EntityUid Solution)? _evaluation;

    private sealed record OxygenEffect(float Amount, float Probability, TemperatureScaling? Temperature,
        EntityCondition[][] Conditions);

    public override void Initialize()
    {
        _prototypes.PrototypesReloaded += OnPrototypesReloaded;
        SubscribeLocalEvent<OxygenMedicineEvaluationComponent, EntityConditionEvent<MobStateCondition>>(OnMobStateCondition,
            after: new[] { typeof(MobStateEntityConditionSystem) });
        SubscribeLocalEvent<OxygenMedicineEvaluationComponent, EntityConditionEvent<ReagentCondition>>(OnReagentCondition);
    }

    public override void Shutdown()
    {
        _prototypes.PrototypesReloaded -= OnPrototypesReloaded;
        base.Shutdown();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args) => _cache.Clear();

    private void OnMobStateCondition(Entity<OxygenMedicineEvaluationComponent> ent, ref EntityConditionEvent<MobStateCondition> args)
    {
        if (_evaluation is { } evaluation && evaluation.Patient == ent.Owner)
            args.Result = evaluation.State == args.Condition.Mobstate;
    }

    private void OnReagentCondition(Entity<OxygenMedicineEvaluationComponent> ent, ref EntityConditionEvent<ReagentCondition> args)
    {
        if (_evaluation is { } evaluation && evaluation.Patient == ent.Owner)
            args.Result = _conditions.RaiseConditionEvent(evaluation.Solution, args.Condition, args.SourceEnt);
    }

    /// <summary>
    /// Returns a capped therapeutic bonus scaled by the available fraction of each shock dose.
    /// </summary>
    public float GetShockHealing(EntityUid patient, Entity<SolutionComponent> solution,
        List<ProtoId<ReagentPrototype>>? medicines = null)
    {
        var added = !HasComp<OxygenMedicineEvaluationComponent>(patient);
        if (added)
            AddComp<OxygenMedicineEvaluationComponent>(patient);
        try
        {
            medicines?.Clear();
            return CalculateHealing(patient, solution, medicines);
        }
        finally
        {
            if (added)
                RemComp<OxygenMedicineEvaluationComponent>(patient);
        }
    }

    private float CalculateHealing(EntityUid patient, Entity<SolutionComponent> solution,
        List<ProtoId<ReagentPrototype>>? medicines)
    {
        var contents = solution.Comp.Solution;
        var generalMedicines = 0;
        var generalHealing = 0f;
        var criticalHealing = 0f;
        ProtoId<ReagentPrototype>? criticalMedicine = null;
        var counted = new HashSet<ProtoId<ReagentPrototype>>();
        foreach (var quantity in contents.Contents)
        {
            var id = quantity.Reagent.Prototype;
            if (!counted.Add(id))
                continue;

            var dose = contents.GetTotalPrototypeQuantity(id).Float();
            if (dose <= 0f)
                continue;

            var effects = GetEffects(id);
            if (effects.Length == 0 ||
                !(Heals(patient, solution, effects, MobState.SoftCrit) || Heals(patient, solution, effects, MobState.Critical)))
                continue;

            var fraction = Math.Min(dose / ShockDose, 1f);
            if (!Heals(patient, solution, effects, MobState.Alive))
            {
                var healing = 40f * fraction;
                if (healing > criticalHealing)
                {
                    criticalHealing = healing;
                    criticalMedicine = id;
                }
            }
            else if (generalMedicines < 2)
            {
                medicines?.Add(id);
                generalHealing += 20f * fraction;
                generalMedicines++;
            }

            if (generalHealing >= 40f || criticalHealing >= 40f)
                break;
        }

        if (criticalMedicine is { } critical && criticalHealing >= generalHealing)
        {
            medicines?.Clear();
            medicines?.Add(critical);
            return criticalHealing;
        }

        return generalHealing;
    }

    private bool Heals(EntityUid patient, EntityUid solution, OxygenEffect[] effects, MobState state)
    {
        var previous = _evaluation;
        _evaluation = (patient, state, solution);
        try
        {
            var total = 0f;
            foreach (var effect in effects)
            {
                var eligible = true;
                foreach (var conditions in effect.Conditions)
                {
                    if (_conditions.TryConditions(patient, conditions))
                        continue;
                    eligible = false;
                    break;
                }

                if (!eligible)
                    continue;

                var amount = effect.Amount * effect.Probability;
                if (effect.Temperature is { } scaling)
                {
                    if (!TryComp<TemperatureComponent>(patient, out var temperature))
                        continue;
                    amount *= (float) scaling.GetEfficiencyMultiplier(temperature.Temperature, 1f, false);
                }

                total += amount;
            }

            return total < 0;
        }
        finally
        {
            _evaluation = previous;
        }
    }

    private OxygenEffect[] GetEffects(ProtoId<ReagentPrototype> id)
    {
        if (_cache.TryGetValue(id, out var cached))
            return cached;

        var result = new List<OxygenEffect>();
        if (_prototypes.TryIndex(id, out var reagent) && reagent.Metabolisms is { } metabolisms &&
            metabolisms.Metabolisms.TryGetValue("Bloodstream", out var bloodstream))
        {
            foreach (var effect in bloodstream.Effects)
                Collect(effect, 1f, [], new HashSet<ProtoId<EntityEffectPrototype>>(), result);
        }

        return _cache[id] = result.ToArray();
    }

    private void Collect(EntityEffect effect, float probability, EntityCondition[][] parents,
        HashSet<ProtoId<EntityEffectPrototype>> path, List<OxygenEffect> result)
    {
        probability *= effect.Probability;
        if (probability <= 0)
            return;

        EntityCondition[][] conditions = effect.Conditions is { Length: > 0 } local ? [..parents, local] : parents;
        if (effect is NestedEffect nested)
        {
            if (!path.Add(nested.Proto))
                return;
            var prototype = _prototypes.Index(nested.Proto);
            if (prototype.Conditions is { Length: > 0 } inherited)
                conditions = [..conditions, inherited];
            foreach (var child in prototype.Effects)
                Collect(child, probability, conditions, path, result);
            path.Remove(nested.Proto);
            return;
        }

        float amount;
        TemperatureScaling? temperature = null;
        switch (effect)
        {
            case HealthChange health:
                amount = (float) health.Damage.DamageDict.GetValueOrDefault("Asphyxiation");
                temperature = health.ScaleByTemperature;
                break;
            case EvenHealthChange even:
                amount = 0f;
                foreach (var (group, change) in even.Damage)
                {
                    var types = _prototypes.Index(group).DamageTypes;
                    if (types.Contains("Asphyxiation"))
                        amount += (float) change / types.Count;
                }
                temperature = even.ScaleByTemperature;
                break;
            case DistributedHealthChange distributed:
                amount = 0f;
                foreach (var (group, change) in distributed.Damage)
                {
                    var types = _prototypes.Index(group).DamageTypes;
                    if (types.Contains("Asphyxiation"))
                        amount += (float) change / types.Count;
                }
                break;
            default:
                return;
        }

        if (amount != 0)
            result.Add(new OxygenEffect(amount, probability, temperature, conditions));
    }
}
