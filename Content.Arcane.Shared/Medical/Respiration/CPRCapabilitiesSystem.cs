using Content.Arcane.Common.Medical.Respiration;
using Content.Medical.Common.Traumas;
using Content.Shared.Body;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.EntitySystems;
using Content.Trauma.Common.Body;
using Content.Trauma.Common.MartialArts;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Shared.Medical.Respiration;

public sealed partial class CPRCapabilitiesSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    private static readonly ProtoId<OrganCategoryPrototype> Lungs = "Lungs";

    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateComponent, CPRCapabilitiesEvent>(OnCapabilities);
    }

    private void OnCapabilities(Entity<MobStateComponent> ent, ref CPRCapabilitiesEvent args)
    {
        var hasRespirator = TryComp<CPRRespirationComponent>(ent, out var targetRespiration);
        args.Handled = true;
        args.CanPerform = ent.Owner != args.User && ent.Comp.CurrentState != MobState.Alive &&
            hasRespirator && _body.GetOrgan(ent, Lungs) != null;
        args.CanVentilate = args.CanPerform && !targetRespiration!.BreathingImmune &&
            TryComp<CPRRespirationComponent>(args.User, out var userRespiration) &&
            userRespiration.HasBreathReserve && !userRespiration.BreathingImmune &&
            !HasComp<BlockedBreathingComponent>(args.User) &&
            (!TryComp<PullableComponent>(args.User, out var pullable) || pullable.GrabStage != GrabStage.Suffocate) &&
            HealthyLungs(args.User) && HealthyLungs(ent) &&
            MouthAvailable(args.User, args.User) && MouthAvailable(args.User, ent);
        args.VerbText = Loc.GetString(args.CanVentilate ? "cpr-verb" : "arcane-chest-compressions-verb");
    }

    private bool HealthyLungs(EntityUid patient)
        => _body.GetOrgan(patient, Lungs) is { } lungs &&
            TryComp<InternalChildOrganComponent>(lungs, out var organ) && organ.OrganSeverity == OrganSeverity.Normal;

    private bool MouthAvailable(EntityUid user, EntityUid target)
    {
        var attempt = new IngestionAttemptEvent(IngestionSystem.DefaultFlags, user);
        RaiseLocalEvent(target, ref attempt);
        return !attempt.Cancelled;
    }
}
