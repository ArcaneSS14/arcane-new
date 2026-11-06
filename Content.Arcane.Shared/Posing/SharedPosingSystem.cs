using Content.Arcane.Common.Input;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs;
using Content.Shared.Movement.Events;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Arcane.Shared.Posing;

public abstract partial class SharedPosingSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private StandingStateSystem _standing = default!;

    private const float OffsetStep = 0.05f;
    private const float AngleStep = 5f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PosingComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<PosingComponent, DownedEvent>(OnDowned);
        SubscribeLocalEvent<PosingComponent, MobStateChangedEvent>(OnMobStateChanged);

        CommandBinds.Builder
            .Bind(ArcaneKeyFunctions.TogglePosing, InputCmdHandler.FromDelegate(OnTogglePosing, handle: false))
            .Bind(ArcaneKeyFunctions.PosingOffsetRight, OffsetHandler(new Vector2(OffsetStep, 0f)))
            .Bind(ArcaneKeyFunctions.PosingOffsetLeft, OffsetHandler(new Vector2(-OffsetStep, 0f)))
            .Bind(ArcaneKeyFunctions.PosingOffsetUp, OffsetHandler(new Vector2(0f, OffsetStep)))
            .Bind(ArcaneKeyFunctions.PosingOffsetDown, OffsetHandler(new Vector2(0f, -OffsetStep)))
            .Bind(ArcaneKeyFunctions.PosingRotatePositive, AngleHandler(-AngleStep))
            .Bind(ArcaneKeyFunctions.PosingRotateNegative, AngleHandler(AngleStep))
            .Register<SharedPosingSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        CommandBinds.Unregister<SharedPosingSystem>();
    }

    private InputCmdHandler OffsetHandler(Vector2 offset)
    {
        return InputCmdHandler.FromDelegate(session =>
            {
                if (session?.AttachedEntity is { } user)
                    TryAdjustOffset(user, offset);
            },
            handle: false);
    }

    private InputCmdHandler AngleHandler(float angle)
    {
        return InputCmdHandler.FromDelegate(session =>
            {
                if (session?.AttachedEntity is { } user)
                    TryAdjustAngle(user, angle);
            },
            handle: false);
    }

    private void OnTogglePosing(ICommonSession? session)
    {
        if (session?.AttachedEntity is { } user && CanTogglePosing(user))
            TogglePosing(user);
    }

    private void OnUpdateCanMove(Entity<PosingComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (ent.Comp.Posing)
            args.Cancel();
    }

    private void OnDowned(Entity<PosingComponent> ent, ref DownedEvent args)
    {
        if (ent.Comp.Posing)
            TogglePosing(ent, ent.Comp);
    }

    private void OnMobStateChanged(Entity<PosingComponent> ent, ref MobStateChangedEvent args)
    {
        if (ent.Comp.Posing)
            TogglePosing(ent, ent.Comp);
    }

    private void TogglePosing(EntityUid uid, PosingComponent? posing = null)
    {
        if (!Resolve(uid, ref posing, false))
            return;

        posing.Posing = !posing.Posing;
        posing.CurrentAngle = Angle.Zero;
        posing.CurrentOffset = Vector2.Zero;
        _actionBlocker.UpdateCanMove(uid);

        OnPosingToggled(uid, posing);
        Dirty(uid, posing);
    }

    private void TryAdjustOffset(EntityUid uid, Vector2 offset, PosingComponent? posing = null)
    {
        if (!Resolve(uid, ref posing, false) || !posing.Posing)
            return;

        var newOffset = Vector2.Clamp(posing.CurrentOffset + offset, -posing.OffsetLimits, posing.OffsetLimits);
        if (newOffset.Equals(posing.CurrentOffset))
            return;

        posing.CurrentOffset = newOffset;
        Dirty(uid, posing);
    }

    private void TryAdjustAngle(EntityUid uid, float angle, PosingComponent? posing = null)
    {
        if (!Resolve(uid, ref posing, false) || !posing.Posing)
            return;

        var degrees = Math.Clamp(posing.CurrentAngle.Degrees + angle, -posing.AngleLimits, posing.AngleLimits);
        var newAngle = Angle.FromDegrees(degrees);
        if (newAngle.Equals(posing.CurrentAngle))
            return;

        posing.CurrentAngle = newAngle;
        Dirty(uid, posing);
    }

    /// <summary>
    /// Called after posing is toggled, for client-side visuals and input context.
    /// </summary>
    protected virtual void OnPosingToggled(EntityUid uid, PosingComponent posing)
    {
    }

    private bool CanTogglePosing(EntityUid uid)
    {
        if (!_actionBlocker.CanConsciouslyPerformAction(uid))
            return false;

        if (TryComp<StaminaComponent>(uid, out var stamina) && stamina.Critical)
            return false;

        return !HasComp<StunnedComponent>(uid) && !_standing.IsDown(uid);
    }
}
