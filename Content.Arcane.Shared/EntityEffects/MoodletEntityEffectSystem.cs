using Content.Arcane.Shared.Mood;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Shared.EntityEffects;

/// <summary>
///     Adds, removes and purges moodlets of an entity with mood.
/// </summary>
public sealed partial class MoodletEntityEffectSystem : EntityEffectSystem<NetMoodComponent, ChemAddMoodlet>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NetMoodComponent, EntityEffectEvent<ChemRemoveMoodlet>>(OnRemove);
        SubscribeLocalEvent<NetMoodComponent, EntityEffectEvent<ChemPurgeMoodlets>>(OnPurge);
    }

    protected override void Effect(Entity<NetMoodComponent> entity, ref EntityEffectEvent<ChemAddMoodlet> args)
    {
        RaiseLocalEvent(entity, new MoodEffectEvent(args.Effect.MoodPrototype));
    }

    private void OnRemove(Entity<NetMoodComponent> entity, ref EntityEffectEvent<ChemRemoveMoodlet> args)
    {
        RaiseLocalEvent(entity, new MoodRemoveEffectEvent(args.Effect.MoodPrototype));
    }

    private void OnPurge(Entity<NetMoodComponent> entity, ref EntityEffectEvent<ChemPurgeMoodlets> args)
    {
        RaiseLocalEvent(entity, new MoodPurgeEffectsEvent(args.Effect.RemovePermanentMoodlets));
    }
}

/// <summary>
///     Adds a moodlet to an entity.
/// </summary>
public sealed partial class ChemAddMoodlet : EntityEffectBase<ChemAddMoodlet>
{
    [DataField(required: true)]
    public ProtoId<MoodEffectPrototype> MoodPrototype;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var mood = prototype.Index(MoodPrototype);
        return Loc.GetString("reagent-effect-guidebook-add-moodlet",
            ("amount", mood.MoodChange),
            ("timeout", mood.Timeout));
    }
}

/// <summary>
///     Removes a moodlet from an entity.
/// </summary>
public sealed partial class ChemRemoveMoodlet : EntityEffectBase<ChemRemoveMoodlet>
{
    [DataField(required: true)]
    public ProtoId<MoodEffectPrototype> MoodPrototype;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-remove-moodlet",
            ("name", prototype.Index(MoodPrototype).Description()));
    }
}

/// <summary>
///     Removes all non-permanent moodlets of an entity.
/// </summary>
public sealed partial class ChemPurgeMoodlets : EntityEffectBase<ChemPurgeMoodlets>
{
    [DataField]
    public bool RemovePermanentMoodlets;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-purge-moodlets");
    }
}
