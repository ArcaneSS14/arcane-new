using Content.Arcane.Common.CCVar;
using Content.Arcane.Common.Medical.Respiration;
using Content.Shared.Body;
using Robust.Shared.Configuration;

namespace Content.Arcane.Shared.Medical.Respiration;

public sealed partial class BrainOxygenSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    private static readonly ProtoId<OrganCategoryPrototype> BrainCategory = "Brain";

    public bool Enabled => _cfg.GetCVar(ArcaneCVars.ComplexRespiratorEnabled);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodyComponent, CPRCompletedEvent>(OnCPRCompleted);
    }

    public Entity<BrainOxygenComponent>? GetBrain(EntityUid body)
    {
        if (Enabled && _body.GetOrgan(body, BrainCategory) is { } brain &&
            TryComp<BrainOxygenComponent>(brain, out var oxygen) && oxygen.Active)
            return (brain, oxygen);

        return null;
    }

    public static BrainOxygenLevel GetLevel(BrainOxygenComponent brain)
    {
        return brain.AirSaturation switch
        {
            > 0.96f => BrainOxygenLevel.Stable,
            > 0.90f => BrainOxygenLevel.Unstable,
            > 0.80f => BrainOxygenLevel.Dangerous,
            > 0.60f => BrainOxygenLevel.Critical,
            _ => BrainOxygenLevel.Fatal,
        };
    }

    private void OnCPRCompleted(Entity<BodyComponent> ent, ref CPRCompletedEvent args)
    {
        if (Enabled)
            args.AllowRevival = false;
    }
}
