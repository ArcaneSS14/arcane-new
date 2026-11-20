// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Objectives.Components;

// Arcane-Start
using Content.Arcane.Shared.Bitrunning.Components;
using Content.Server.Objectives.Systems;
using Content.Server.Polymorph.Systems;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
// Arcane-End

namespace Content.Trauma.Server.Objectives;

/// <summary>
/// Handles Teach a Lesson logic on if a specific entity has died at least once during the round
/// </summary>
public sealed partial class TeachALessonConditionSystem : EntitySystem // Arcane-Edit
{
    // Arcane-Start
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private TargetObjectiveSystem _objectives = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;
    // Arcane-End

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeachALessonBodyComponent, MobStateChangedEvent>(OnMobStateChanged); // Arcane-Edit
        SubscribeLocalEvent<TeachALessonTargetComponent, MindGotAddedEvent>(OnTargetMoved); // Arcane
        SubscribeLocalEvent<TeachALessonConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
        SubscribeLocalEvent<TeachALessonConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<TeachALessonConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = ent.Comp.HasDied ? 1.0f : 0.0f;
    }

    private void OnAfterAssign(Entity<TeachALessonConditionComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        if (!_objectives.GetTarget(ent.Owner, out var targetMindId) || !TryComp<MindComponent>(targetMindId, out var targetMind)) // Arcane-Edit
            return;

        // Arcane-Edit-Start
        var mind = targetMindId.Value;
        var target = EnsureComp<TeachALessonTargetComponent>(mind);
        target.Teachers.Add(ent);
        WatchCharacter((mind, target), targetMind.OwnedEntity);
        // Arcane-Edit-End
    }

    // Arcane-Start
    private void OnTargetMoved(Entity<TeachALessonTargetComponent> ent, ref MindGotAddedEvent args)
        => WatchCharacter(ent, args.Container.Owner);

    private void WatchCharacter(Entity<TeachALessonTargetComponent> target, EntityUid? entity)
    {
        if (TryComp<GhostComponent>(entity, out var ghost))
        {
            if (!ghost.CanReturnToBody ||
                target.Comp.Body is { } watchedBody && TerminatingOrDeleted(watchedBody))
                Complete(target.Comp);
            return;
        }

        var body = entity;
        for (var current = entity; current is { } candidate; current = _polymorph.GetPolymorphParent(candidate))
        {
            if (!TryComp<AvatarConnectionComponent>(candidate, out var avatar))
                continue;

            body = avatar.OriginalBody;
            break;
        }
        if (body is not { } uid || TerminatingOrDeleted(uid))
            return;

        if (target.Comp.Body != uid)
        {
            if (TryComp<TeachALessonBodyComponent>(target.Comp.Body, out var previous))
                previous.Minds.Remove(target.Owner);

            EnsureComp<TeachALessonBodyComponent>(uid).Minds.Add(target.Owner);
            target.Comp.Body = uid;
        }

        if (_mobState.IsDead(uid))
            Complete(target.Comp);
    }

    // Arcane-End

    private void OnMobStateChanged(Entity<TeachALessonBodyComponent> ent, ref MobStateChangedEvent args) // Arcane-Edit
    {
        if (args.NewMobState != MobState.Dead) // Arcane-Edit
            return;

        // Arcane-Edit-Start
        foreach (var mind in ent.Comp.Minds)
        {
            if (TryComp<TeachALessonTargetComponent>(mind, out var target) && target.Body == ent.Owner)
                Complete(target);
        }
        // Arcane-Edit-End
    }

    // Arcane-Start
    private void Complete(TeachALessonTargetComponent target)
    {
        target.Teachers.RemoveWhere(teacher => !HasComp<TeachALessonConditionComponent>(teacher));
        foreach (var teacher in target.Teachers)
        {
            if (!TryComp<TeachALessonConditionComponent>(teacher, out var condition))
                continue;

            condition.HasDied = true;
        }
    }
    // Arcane-End
}
