using Content.Arcane.Common.Medical.Respiration;
using Content.Arcane.Shared.Medical.Respiration;
using Content.Goobstation.Common.Body.Components;
using Content.Medical.Common.Body;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;

namespace Content.Arcane.Server.Medical.Respiration;

public sealed partial class CPRRespirationSystem : EntitySystem
{
    [Dependency] private RespiratorSystem _respirator = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RespiratorComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RespiratorComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RespiratorComponent, BreathingReserveChangedEvent>(OnReserveChanged);
        SubscribeLocalEvent<BreathingImmunityComponent, ComponentStartup>(OnImmunityStartup);
        SubscribeLocalEvent<BreathingImmunityComponent, ComponentShutdown>(OnImmunityShutdown);
        SubscribeLocalEvent<SpecialBreathingImmunityComponent, ComponentStartup>(OnImmunityStartup);
        SubscribeLocalEvent<SpecialBreathingImmunityComponent, ComponentShutdown>(OnImmunityShutdown);
    }

    private void OnStartup(Entity<RespiratorComponent> ent, ref ComponentStartup args)
        => _respirator.UpdateSaturation(ent.Owner, 0, ent.Comp);

    private void OnShutdown(Entity<RespiratorComponent> ent, ref ComponentShutdown args)
        => RemComp<CPRRespirationComponent>(ent);

    private void OnImmunityStartup<T>(Entity<T> ent, ref ComponentStartup args) where T : Component
        => Refresh(ent.Owner);

    private void OnImmunityShutdown<T>(Entity<T> ent, ref ComponentShutdown args) where T : Component
        => Refresh(ent.Owner);

    private void Refresh(EntityUid uid)
    {
        if (TryComp<RespiratorComponent>(uid, out var respirator) && respirator.LifeStage <= ComponentLifeStage.Running)
            _respirator.UpdateSaturation(uid, 0, respirator);
    }

    private void OnReserveChanged(Entity<RespiratorComponent> ent, ref BreathingReserveChangedEvent args)
    {
        var state = EnsureComp<CPRRespirationComponent>(ent);
        var immune = Active<BreathingImmunityComponent>(ent) || Active<SpecialBreathingImmunityComponent>(ent);
        if (state.HasBreathReserve == args.HasReserve && state.BreathingImmune == immune)
            return;

        state.HasBreathReserve = args.HasReserve;
        state.BreathingImmune = immune;
        Dirty(ent.Owner, state);
        var changed = new RespirationStateChangedEvent();
        RaiseLocalEvent(ent.Owner, ref changed);
    }

    private bool Active<T>(EntityUid uid) where T : Component
        => TryComp<T>(uid, out var component) && component.LifeStage <= ComponentLifeStage.Running;
}
