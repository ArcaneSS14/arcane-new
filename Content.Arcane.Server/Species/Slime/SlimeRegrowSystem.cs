using System.Linq;
using Content.Arcane.Shared.Species.Slime;
using Content.Medical.Common.Traumas;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Traumas;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Arcane.Server.Species.Slime;

public sealed partial class SlimeRegrowSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private InitialBodySystem _initialBody = default!;
    [Dependency] private BodyPartSystem _parts = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private TraumaSystem _trauma = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;

    private const float HungerCost = 60f;
    private const float ThirstCost = 100f;
    private static readonly EntProtoId RegrowAction = "ActionSlimeRegrowLimb";
    private static readonly SoundSpecifier RegrowSound = new SoundPathSpecifier("/Audio/Voice/Slime/slime_squish.ogg");
    private static readonly ProtoId<OrganCategoryPrototype> Head = "Head";
    private static readonly ProtoId<OrganCategoryPrototype>[] HeadOrgans = ["Eyes", "Tongue"];
    private static readonly ProtoId<OrganCategoryPrototype>[] RegrowCategories =
    [
        Head, "ArmLeft", "ArmRight", "HandLeft", "HandRight",
        "LegLeft", "LegRight", "FootLeft", "FootRight",
    ];

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<SlimeRegrowComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.Action, RegrowAction);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<SlimeRegrowComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.Action);
    }

    [SubscribeLocalEvent]
    private void OnOrganRemoved(Entity<SlimeRegrowComponent> ent, ref OrganRemovedFromEvent args)
    {
        if (TerminatingOrDeleted(ent) ||
            _body.GetCategory(args.Organ.AsNullable()) is not { } category ||
            !RegrowCategories.Contains(category) && !HeadOrgans.Contains(category))
            return;

        if (TryComp<VisualOrganComponent>(args.Organ, out var visual))
            ent.Comp.LostProfiles[category] = visual.Profile;

        if (TryComp<VisualOrganMarkingsComponent>(args.Organ, out var markings))
            ent.Comp.LostMarkings[category] = markings.Markings.ToDictionary(pair => pair.Key, pair => pair.Value.ToList());
    }

    [SubscribeLocalEvent]
    private void OnRegrow(Entity<SlimeRegrowComponent> ent, ref SlimeRegrowActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner || ent.Comp.Regrowing || !_mobState.IsAlive(ent))
            return;

        var candidates = new List<(Entity<BodyPartComponent> Parent, ProtoId<OrganCategoryPrototype> Slot)>();
        foreach (var part in _parts.GetBodyParts(ent.Owner))
        {
            foreach (var slot in RegrowCategories)
            {
                if (_parts.CanInsertOrgan(part.AsNullable(), slot) &&
                    _body.GetOrgan(ent, slot) == null &&
                    _initialBody.HasInitialOrgan(ent.Owner, slot) == true)
                    candidates.Add((part, slot));
            }
        }

        if (candidates.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("slime-regrow-limb-none"), ent, ent);
            return;
        }

        if (!TryComp<SatiationComponent>(ent, out var satiation) ||
            _satiation.GetValueOrNull((ent, satiation), SatiationSystem.Hunger) is not { } hunger || hunger < HungerCost)
        {
            _popup.PopupEntity(Loc.GetString("slime-regrow-limb-too-hungry"), ent, ent);
            return;
        }

        if (_satiation.GetValueOrNull((ent, satiation), SatiationSystem.Thirst) is not { } thirst || thirst < ThirstCost)
        {
            _popup.PopupEntity(Loc.GetString("slime-regrow-limb-too-thirsty"), ent, ent);
            return;
        }

        var (parent, slotToGrow) = _random.Pick(candidates);
        ent.Comp.Regrowing = true;
        try
        {
            if (!_parts.RestoreInitialChild(parent.AsNullable(), slotToGrow, recursive: false))
                return;

            _satiation.ModifyValue((ent, satiation), SatiationSystem.Hunger, -HungerCost);
            _satiation.ModifyValue((ent, satiation), SatiationSystem.Thirst, -ThirstCost);
            RestoreAppearance(ent, slotToGrow);
            RemoveStumpTrauma(parent.Owner, slotToGrow);

            if (slotToGrow == Head && _parts.GetOrgan(parent.AsNullable(), Head) is { } head)
                RestoreHeadOrgans(ent, head);

            args.Handled = true;
            _popup.PopupEntity(Loc.GetString("slime-regrow-limb-success"), ent, ent);
            _audio.PlayEntity(RegrowSound, ent, ent);
        }
        finally
        {
            ent.Comp.Regrowing = false;
        }
    }

    private void RestoreHeadOrgans(Entity<SlimeRegrowComponent> ent, EntityUid head)
    {
        foreach (var slot in HeadOrgans)
        {
            if (_body.GetOrgan(ent, slot) == null &&
                _parts.CanInsertOrgan(head, slot) &&
                _parts.RestoreInitialChild(head, slot, recursive: false))
                RestoreAppearance(ent, slot);
        }
    }

    private void RestoreAppearance(Entity<SlimeRegrowComponent> ent, ProtoId<OrganCategoryPrototype> slot)
    {
        if (ent.Comp.LostProfiles.Remove(slot, out var profile))
            _visualBody.ApplyProfiles(ent, new() { [slot] = profile });

        if (ent.Comp.LostMarkings.Remove(slot, out var markings))
            _visualBody.ApplyMarkings(ent, new() { [slot] = markings });
    }

    private void RemoveStumpTrauma(EntityUid parent, ProtoId<OrganCategoryPrototype> slot)
    {
        if (!_trauma.GetPartTraumas(parent, out var traumas, TraumaType.Dismemberment))
            return;

        foreach (var trauma in traumas.ToArray())
        {
            if (TryComp<AmputationTraumaComponent>(trauma, out var amputation) && amputation.Source == slot)
                _trauma.RemoveTrauma(trauma);
        }
    }
}
