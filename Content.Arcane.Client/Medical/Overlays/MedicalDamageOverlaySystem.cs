using Content.Arcane.Shared.Medical.Overlays;
using Content.Client.Overlays;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Arcane.Client.Medical.Overlays;

public sealed partial class MedicalDamageOverlaySystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private BlackAndWhiteOverlaySystem _overlay = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    private static readonly ProtoId<DamageGreyscalePrototype> Effect = "BloodlossGreyscale";
    private bool _refreshRequired;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnDetached);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<DamageableComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<DamageableComponent, ComponentShutdown>(OnDamageableShutdown);
        SubscribeLocalEvent<BloodstreamComponent, ComponentStartup>(OnBloodstreamStartup);
        SubscribeLocalEvent<BloodstreamComponent, ComponentShutdown>(OnBloodstreamShutdown);
        SubscribeLocalEvent<DamageableComponent, OrganGotInsertedEvent>(OnOrganInserted);
        SubscribeLocalEvent<DamageableComponent, OrganGotRemovedEvent>(OnOrganRemoved);
    }

    public override void Shutdown()
    {
        Reset();
        base.Shutdown();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        if (!_refreshRequired)
            return;

        _refreshRequired = false;
        Refresh();
    }

    private void OnAttached(LocalPlayerAttachedEvent args)
    {
        _refreshRequired = true;
    }

    private void OnDetached(LocalPlayerDetachedEvent args)
    {
        Reset();
        _refreshRequired = true;
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        Reset();
    }

    private void OnDamageChanged(Entity<DamageableComponent> ent, ref DamageChangedEvent args)
    {
        MarkDirty(ent);
    }

    private void OnDamageableShutdown(Entity<DamageableComponent> ent, ref ComponentShutdown args)
    {
        MarkDirty(ent);
    }

    private void OnBloodstreamStartup(Entity<BloodstreamComponent> ent, ref ComponentStartup args)
    {
        MarkDirty(ent);
    }

    private void OnBloodstreamShutdown(Entity<BloodstreamComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Owner == _player.LocalEntity)
        {
            _overlay.SetIntensity(this, 0f);
            _refreshRequired = true;
        }
    }

    private void OnOrganInserted(Entity<DamageableComponent> ent, ref OrganGotInsertedEvent args)
    {
        _refreshRequired |= args.Target == _player.LocalEntity;
    }

    private void OnOrganRemoved(Entity<DamageableComponent> ent, ref OrganGotRemovedEvent args)
    {
        _refreshRequired |= args.Target == _player.LocalEntity;
    }

    private void MarkDirty(EntityUid entity)
    {
        if (_refreshRequired)
            return;

        if (_player.LocalEntity is { } player && (entity == player || _body.GetBody(entity) == player))
            _refreshRequired = true;
    }

    private void Reset()
    {
        _refreshRequired = false;
        _overlay.SetIntensity(this, 0f);
        var ev = new LocalPlayerMedicalDamageEvent(null, new());
        RaiseLocalEvent(ref ev);
    }

    private void Refresh()
    {
        if (_player.LocalEntity is not { } player
            || !TryComp<DamageableComponent>(player, out var damageable))
        {
            Reset();
            return;
        }

        var effect = ProtoMan.Index(Effect);
        var damage = _damageable.GetAllDamage((player, damageable));
        var total = FixedPoint2.Zero;
        if (TryComp<BloodstreamComponent>(player, out var blood) && blood.LifeStage < ComponentLifeStage.Stopping)
        {
            foreach (var type in effect.DamageTypes)
                total += damage.DamageDict.GetValueOrDefault(type);
        }

        var intensity = effect.FullIntensityDamage > 0f ? total.Float() / effect.FullIntensityDamage : 0f;
        _overlay.SetIntensity(this, intensity);
        var ev = new LocalPlayerMedicalDamageEvent(player, damage);
        RaiseLocalEvent(ref ev);
    }
}

/// <summary>
/// One batched damage snapshot shared by the local player's medical overlays.
/// </summary>
[ByRefEvent]
public readonly record struct LocalPlayerMedicalDamageEvent(EntityUid? Player, DamageSpecifier Damage);
