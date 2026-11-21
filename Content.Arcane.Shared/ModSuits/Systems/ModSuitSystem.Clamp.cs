using Content.Arcane.Shared.ModSuits.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Storage.Components;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    private const string ClampContainerId = "mod-clamp";
    [Dependency] private SharedDoAfterSystem _clampDoAfter = default!;

    private void InitializeClamp()
    {
        SubscribeLocalEvent<ModClampComponent, ComponentInit>(OnClampInit);
        SubscribeLocalEvent<ModClampComponent, AfterInteractEvent>(OnClampInteract);
        SubscribeLocalEvent<ModClampComponent, ModClampGrabDoAfterEvent>(OnClampGrab);
        SubscribeLocalEvent<ModClampComponent, ModClampDropDoAfterEvent>(OnClampDrop);
        SubscribeLocalEvent<ModClampComponent, EntityTerminatingEvent>(OnClampTerminating);
    }

    private void OnClampInit(Entity<ModClampComponent> clamp, ref ComponentInit args) =>
        clamp.Comp.Cargo = _containers.EnsureContainer<Container>(clamp, ClampContainerId);

    private void OnClampInteract(Entity<ModClampComponent> clamp, ref AfterInteractEvent args)
    {
        if (_net.IsClient || args.Handled || !args.CanReach || !CanOperateClamp(clamp, args.User))
            return;

        DoAfterArgs doAfter;
        if (args.Target is { } target && CanClampGrab(clamp, target, args.User))
        {
            if (clamp.Comp.Cargo.Count >= clamp.Comp.MaxContents)
            {
                _popup.PopupEntity(Loc.GetString("modsuit-clamp-full"), clamp, args.User);
                args.Handled = true;
                return;
            }

            doAfter = new DoAfterArgs(EntityManager, args.User, clamp.Comp.GrabDelay,
                new ModClampGrabDoAfterEvent(), clamp, target, clamp);
        }
        else
        {
            if (clamp.Comp.Cargo.Count == 0)
                return;

            doAfter = new DoAfterArgs(EntityManager, args.User, clamp.Comp.DropDelay,
                new ModClampDropDoAfterEvent(GetNetCoordinates(args.ClickLocation)), clamp, used: clamp);
        }

        doAfter.BreakOnMove = true;
        doAfter.BreakOnDamage = true;
        doAfter.NeedHand = true;
        if (_clampDoAfter.TryStartDoAfter(doAfter))
            _audio.PlayPvs(clamp.Comp.Sound, clamp);
        args.Handled = true;
    }

    private bool CanOperateClamp(EntityUid clamp, EntityUid user) =>
        TryComp(clamp, out ModDeviceItemComponent? device)
        && TryComp(device.Module, out ModModuleComponent? module)
        && module.Active && TryComp(module.Suit, out ModSuitComponent? suit) && suit.Wearer == user
        && _hands.IsHolding(user, clamp) && _actionBlocker.CanInteract(user, clamp);

    private bool CanClampGrab(Entity<ModClampComponent> clamp, EntityUid target, EntityUid user) =>
        target != user && target != clamp.Owner && !TerminatingOrDeleted(target) && HasComp<EntityStorageComponent>(target)
        && !Transform(target).Anchored && !_containers.IsEntityInContainer(target)
        && (!TryComp(target, out PhysicsComponent? physics) || physics.BodyType != BodyType.Static)
        && _interaction.InRangeUnobstructed(user, target);

    private void OnClampGrab(Entity<ModClampComponent> clamp, ref ModClampGrabDoAfterEvent args)
    {
        if (_net.IsClient || args.Cancelled || args.Handled || args.Target is not { } target
            || clamp.Comp.Cargo.Count >= clamp.Comp.MaxContents
            || !CanOperateClamp(clamp, args.User) || !CanClampGrab(clamp, target, args.User))
            return;

        args.Handled = _containers.Insert(target, clamp.Comp.Cargo);
    }

    private void OnClampDrop(Entity<ModClampComponent> clamp, ref ModClampDropDoAfterEvent args)
    {
        if (_net.IsClient || args.Cancelled || args.Handled || clamp.Comp.Cargo.Count == 0
            || !CanOperateClamp(clamp, args.User))
            return;

        var destination = GetCoordinates(args.Destination);
        if (!destination.IsValid(EntityManager) || !_interaction.InRangeUnobstructed(args.User, destination))
            return;

        var item = clamp.Comp.Cargo.ContainedEntities[^1];
        args.Handled = _containers.Remove(item, clamp.Comp.Cargo, destination: destination);
    }

    private void OnClampTerminating(Entity<ModClampComponent> clamp, ref EntityTerminatingEvent args) =>
        ReleaseClampCargo(clamp);

    private void ReleaseClampCargo(EntityUid device)
    {
        if (!_net.IsClient && TryComp(device, out ModClampComponent? clamp) && clamp.Cargo.Count > 0)
            _containers.EmptyContainer(clamp.Cargo, true, _transform.GetMoverCoordinates(device));
    }
}
