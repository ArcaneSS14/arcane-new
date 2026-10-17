using Content.Arcane.Common.Medical.HealthAnalyzer;
using Content.Arcane.Shared.Medical.Heart;
using Content.Shared.Body;
using Content.Shared.IdentityManagement;

namespace Content.Arcane.Client.Medical.Heart;

public sealed partial class HeartRateAnalyzerSystem : EntitySystem
{
    [Dependency] private HeartRateSystem _heartRate = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodyComponent, HealthAnalyzerPopulateEvent>(OnPopulate);
    }

    private void OnPopulate(Entity<BodyComponent> ent, ref HealthAnalyzerPopulateEvent args)
    {
        if (_heartRate.GetHeart(ent) is not { } heartEntity)
        {
            if (_heartRate.RequiresHeart(ent))
            {
                args.Pulse = Loc.GetString("health-analyzer-window-entity-bpm-stopped-text");
                AddCondition(args, "condition-heart-missing", ent);
            }
            return;
        }

        if (heartEntity.Comp is not { } heart)
            return;

        var state = _heartRate.GetState(heart);
        args.Pulse = state == HeartState.Stopped
            ? Loc.GetString("health-analyzer-window-entity-bpm-stopped-text")
            : Loc.GetString("health-analyzer-window-entity-bpm-value-text", ("bpm", MathF.Round(heart.CurrentRate)));

        var key = state switch
        {
            HeartState.Stopped => "condition-heart-stopped",
            HeartState.Fibrillating => "condition-heart-fibrillating",
            _ => null,
        };
        if (key == null)
            return;

        AddCondition(args, key, ent);
    }

    private void AddCondition(HealthAnalyzerPopulateEvent args, string key, EntityUid body)
    {
        args.Conditions.Add(Loc.GetString(key, ("entity", Identity.Name(body, EntityManager))));
    }
}
