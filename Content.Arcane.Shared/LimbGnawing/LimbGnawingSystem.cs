using Content.Medical.Common.Body;
using Content.Medical.Shared.Body;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Random;

namespace Content.Arcane.Shared.LimbGnawing;

public sealed partial class LimbGnawingSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private BodyPartSystem _parts = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private static readonly BodyPartType[] GnawableTypes =
        [BodyPartType.Hand, BodyPartType.Foot, BodyPartType.Arm, BodyPartType.Leg];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LimbGnawingTargetComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<LimbGnawingTargetComponent, MapInitEvent>(OnMapInit, after: [typeof(InitialBodySystem)]);
        SubscribeLocalEvent<LimbGnawingTargetComponent, OrganInsertedIntoEvent>(OnOrganInserted);
        SubscribeLocalEvent<LimbGnawingTargetComponent, OrganRemovedFromEvent>(OnOrganRemoved);
        SubscribeLocalEvent<GnawableCorpseComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<LimbGnawerComponent, LimbGnawDoAfterEvent>(OnDoAfter);
    }

    private void OnMobStateChanged(Entity<LimbGnawingTargetComponent> ent, ref MobStateChangedEvent args)
    {
        UpdateMarker(ent);
    }

    private void OnMapInit(Entity<LimbGnawingTargetComponent> ent, ref MapInitEvent args)
    {
        UpdateMarker(ent);
    }

    private void OnOrganInserted(Entity<LimbGnawingTargetComponent> ent, ref OrganInsertedIntoEvent args)
    {
        UpdateMarker(ent);
    }

    private void OnOrganRemoved(Entity<LimbGnawingTargetComponent> ent, ref OrganRemovedFromEvent args)
    {
        UpdateMarker(ent);
    }

    private void OnGetVerbs(Entity<GnawableCorpseComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract
            || !TryComp<LimbGnawerComponent>(args.User, out var gnawer)
            || !_mobState.IsAlive(args.User)
            || !_mobState.IsDead(ent)
            || PickPart(ent, gnawer) == null)
            return;

        var user = args.User;
        var target = ent.Owner;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("limb-gnaw-verb"),
            Priority = 10,
            Act = () => StartGnaw(user, gnawer, target),
        });
    }

    private void StartGnaw(EntityUid user, LimbGnawerComponent gnawer, EntityUid target)
    {
        if (!_mobState.IsAlive(user) || !_mobState.IsDead(target)
            || !_interaction.InRangeAndAccessible(user, target)
            || PickPart(target, gnawer) == null)
            return;

        var args = new DoAfterArgs(EntityManager, user, gnawer.Delay, new LimbGnawDoAfterEvent(), user, target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
        };
        if (_doAfter.TryStartDoAfter(args))
            _popup.PopupPredicted(Loc.GetString("limb-gnaw-start", ("user", user), ("target", target)), user, user);
    }

    private void OnDoAfter(Entity<LimbGnawerComponent> ent, ref LimbGnawDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        args.Handled = true;
        if (_net.IsClient || TerminatingOrDeleted(target)
            || !_mobState.IsAlive(ent) || !_mobState.IsDead(target)
            || !_interaction.InRangeAndAccessible(ent.Owner, target)
            || PickPart(target, ent.Comp) is not { } part
            || _body.GetBody(part) != target
            || !_body.RemoveOrgan(target, part)
            || _body.GetBody(part) == target)
            return;

        _popup.PopupEntity(Loc.GetString("limb-gnaw-finish", ("user", ent.Owner), ("part", part)), ent, PopupType.MediumCaution);
        UpdateMarker(target);
    }

    private void UpdateMarker(EntityUid body)
    {
        if (_net.IsClient || TerminatingOrDeleted(body))
            return;

        if (_mobState.IsDead(body))
        {
            foreach (var part in _parts.GetBodyParts(body))
            {
                if (Array.IndexOf(GnawableTypes, part.Comp.PartType) >= 0)
                {
                    EnsureComp<GnawableCorpseComponent>(body);
                    return;
                }
            }
        }
        RemComp<GnawableCorpseComponent>(body);
    }

    private EntityUid? PickPart(EntityUid body, LimbGnawerComponent gnawer)
    {
        var parts = _parts.GetBodyParts(body);
        var candidates = new List<EntityUid>();
        foreach (var type in gnawer.PartTypes)
        {
            foreach (var part in parts)
            {
                if (part.Comp.PartType == type && _body.GetBody(part.Owner) == body)
                    candidates.Add(part.Owner);
            }
            if (candidates.Count > 0)
                return _random.Pick(candidates);
        }
        return null;
    }
}
