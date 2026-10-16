using Content.Arcane.Shared.Medical.Heart;
using Content.Client.HealthAnalyzer.UI;
using Content.Shared.Body;
using Content.Shared.IdentityManagement;
using Robust.Client.UserInterface.Controls;

namespace Content.Arcane.Client.Medical.Heart;

public sealed partial class HeartRateAnalyzerSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private HeartRateSystem _heartRate = default!;

    private static readonly ProtoId<OrganCategoryPrototype> Heart = "Heart";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodyComponent, HealthAnalyzerPopulateEvent>(OnPopulate);
    }

    private void OnPopulate(Entity<BodyComponent> ent, ref HealthAnalyzerPopulateEvent args)
    {
        if (_body.GetOrgan(ent, Heart) is not { } uid)
        {
            if (_heartRate.RequiresHeart(ent))
            {
                args.Pulse.Text = Loc.GetString("health-analyzer-window-entity-bpm-stopped-text");
                AddCondition(args, "condition-heart-missing", ent);
            }
            return;
        }

        if (!TryComp<HeartRateComponent>(uid, out var heart))
            return;

        var state = _heartRate.GetState(heart);
        args.Pulse.Text = state == HeartState.Stopped
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
        args.Conditions.AddChild(new RichTextLabel
        {
            Text = Loc.GetString(key, ("entity", Identity.Name(body, EntityManager))),
            Margin = new Thickness(0, 4),
        });
    }
}
