// Arcane-Start
using System.Linq;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Components;
using Content.Shared.Movement.Components;
using Content.Shared.MouseRotator;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Shared.Interaction.ForcedFacing;

public sealed partial class ForcedFacingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly RotateToFaceSystem _rotateToFace = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForcedFacingUiComponent, BoundUIOpenedEvent>(OnBoundUiOpened);
        SubscribeLocalEvent<ForcedFacingUiComponent, BoundUIClosedEvent>(OnBoundUiClosed);
        SubscribeLocalEvent<UserInterfaceUserComponent, RefreshForcedFacingEvent>(OnRefreshForcedFacingUi);
        SubscribeLocalEvent<DoAfterComponent, RefreshForcedFacingEvent>(OnRefreshForcedFacingDoAfter);
        SubscribeLocalEvent<MouseRotatorComponent, RefreshNoRotateOnMoveEvent>(OnMouseRotatorRefreshNoRotateOnMove);
        SubscribeLocalEvent<ForcedFacingComponent, RefreshNoRotateOnMoveEvent>(OnRefreshNoRotateOnMove);
        SubscribeLocalEvent<ForcedFacingComponent, RefreshNoRotateOnInteractEvent>(OnRefreshNoRotateOnInteract);
        SubscribeLocalEvent<ForcedFacingComponent, ComponentShutdown>(OnForcedFacingShutdown);
        SubscribeLocalEvent<ForcedFacingTargetComponent, ComponentShutdown>(OnForcedFacingTargetShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ForcedFacingComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.PrimaryTarget is not { } target || TerminatingOrDeleted(target))
                continue;

            _rotateToFace.TryFaceCoordinates(uid, _transform.GetMapCoordinates(target).Position);
        }
    }

    public void RefreshForcedFacing(EntityUid uid)
    {
        if (_timing.ApplyingState || TerminatingOrDeleted(uid))
            return;

        var ev = new RefreshForcedFacingEvent();
        RaiseLocalEvent(uid, ref ev);

        if (TryComp(uid, out ForcedFacingComponent? component))
        {
            foreach (var target in component.Targets.ToArray())
            {
                if (!ev.Targets.Contains(target))
                    StopFacing(uid, target);
            }
        }

        if (ev.Targets.Count == 0)
        {
            if (RemComp<ForcedFacingComponent>(uid))
            {
                RefreshNoRotateOnMove(uid);
                RefreshNoRotateOnInteract(uid);
            }

            return;
        }

        component = EnsureComp<ForcedFacingComponent>(uid);
        foreach (var target in ev.Targets)
        {
            if (!component.Targets.Contains(target))
                StartFacing(uid, target);
        }

        Dirty(uid, component);
        RefreshNoRotateOnMove(uid);
        RefreshNoRotateOnInteract(uid);
    }

    private void OnBoundUiOpened(EntityUid uid, ForcedFacingUiComponent component, BoundUIOpenedEvent args)
    {
        RefreshForcedFacing(args.Actor);
    }

    private void OnBoundUiClosed(EntityUid uid, ForcedFacingUiComponent component, BoundUIClosedEvent args)
    {
        RefreshForcedFacing(args.Actor);
    }

    private void OnRefreshForcedFacingUi(EntityUid uid, UserInterfaceUserComponent component, ref RefreshForcedFacingEvent args)
    {
        foreach (var target in component.OpenInterfaces.Keys)
        {
            if (target != uid && HasComp<ForcedFacingUiComponent>(target))
                AddTarget(ref args, target);
        }
    }

    private void OnRefreshForcedFacingDoAfter(EntityUid uid, DoAfterComponent component, ref RefreshForcedFacingEvent args)
    {
        foreach (var doAfter in component.DoAfters.Values)
        {
            if (doAfter.Cancelled || doAfter.Completed || !doAfter.Args.FaceTarget)
                continue;

            if (doAfter.Args.Target is not { } target || target == uid || !Exists(target) || TerminatingOrDeleted(target))
                continue;

            AddTarget(ref args, target);
        }
    }

    private static void AddTarget(ref RefreshForcedFacingEvent args, EntityUid target)
    {
        if (!args.Targets.Contains(target))
            args.Targets.Add(target);
    }

    private void OnMouseRotatorRefreshNoRotateOnMove(EntityUid uid, MouseRotatorComponent component, ref RefreshNoRotateOnMoveEvent args)
    {
        args.Enabled = true;
    }

    private void OnRefreshNoRotateOnMove(EntityUid uid, ForcedFacingComponent component, ref RefreshNoRotateOnMoveEvent args)
    {
        args.Enabled = true;
    }

    private void OnRefreshNoRotateOnInteract(EntityUid uid, ForcedFacingComponent component, ref RefreshNoRotateOnInteractEvent args)
    {
        args.Enabled = true;
    }

    private void OnForcedFacingShutdown(EntityUid uid, ForcedFacingComponent component, ref ComponentShutdown args)
    {
        foreach (var target in component.Targets.ToArray())
            StopFacing(uid, target);
    }

    private void OnForcedFacingTargetShutdown(EntityUid uid, ForcedFacingTargetComponent component, ref ComponentShutdown args)
    {
        foreach (var facing in component.Facing.ToArray())
        {
            if (!TerminatingOrDeleted(facing))
                RefreshForcedFacing(facing);
        }
    }

    private void StartFacing(EntityUid uid, EntityUid target)
    {
        if (TerminatingOrDeleted(uid) || !Exists(target) || TerminatingOrDeleted(target))
            return;

        var component = EnsureComp<ForcedFacingComponent>(uid);
        if (!component.Targets.Contains(target))
            component.Targets.Add(target);

        var targetComponent = EnsureComp<ForcedFacingTargetComponent>(target);
        targetComponent.Facing.Add(uid);
        Dirty(target, targetComponent);
    }

    private void StopFacing(EntityUid uid, EntityUid target)
    {
        if (TryComp(uid, out ForcedFacingComponent? component))
        {
            component.Targets.Remove(target);
            if (!TerminatingOrDeleted(uid))
                Dirty(uid, component);
        }

        if (!TryComp(target, out ForcedFacingTargetComponent? targetComponent))
            return;

        targetComponent.Facing.Remove(uid);
        if (TerminatingOrDeleted(target))
            return;

        if (targetComponent.Facing.Count == 0)
            RemComp<ForcedFacingTargetComponent>(target);
        else
            Dirty(target, targetComponent);
    }

    private void RefreshNoRotateOnMove(EntityUid uid)
    {
        var ev = new RefreshNoRotateOnMoveEvent();
        RaiseLocalEvent(uid, ref ev);

        if (ev.Enabled)
            EnsureComp<NoRotateOnMoveComponent>(uid);
        else if (!HasPrototypeComponent<NoRotateOnMoveComponent>(uid))
            RemComp<NoRotateOnMoveComponent>(uid);
    }

    private void RefreshNoRotateOnInteract(EntityUid uid)
    {
        var ev = new RefreshNoRotateOnInteractEvent();
        RaiseLocalEvent(uid, ref ev);

        if (ev.Enabled)
            EnsureComp<NoRotateOnInteractComponent>(uid);
        else if (!HasPrototypeComponent<NoRotateOnInteractComponent>(uid))
            RemComp<NoRotateOnInteractComponent>(uid);
    }

    private bool HasPrototypeComponent<T>(EntityUid uid) where T : IComponent, new()
    {
        return MetaData(uid).EntityPrototype?.Components.ContainsKey(_factory.GetComponentName<T>()) ?? false;
    }
}
// Arcane-End
