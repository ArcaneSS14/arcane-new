using System.Linq;
using Content.Arcane.Shared.ModSuits;
using Content.Arcane.Shared.ModSuits.Components;
using Content.Arcane.Shared.ModSuits.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Forensics.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Temperature.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;

namespace Content.Arcane.Server.ModSuits;

public sealed partial class ModStatusReadoutSystem : EntitySystem
{
    [Dependency] private ModSuitSystem _suits = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private BloodstreamSystem _blood = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private readonly HashSet<EntityUid> _open = [];
    private float _untilRefresh;

    public override void Initialize()
    {
        SubscribeLocalEvent<ModStatusReadoutComponent, ModModuleUsedEvent>(OnUsed);
        SubscribeLocalEvent<ModStatusReadoutComponent, BoundUIOpenedEvent>(OnOpen);
        SubscribeLocalEvent<ModStatusReadoutComponent, BoundUIClosedEvent>(OnClose);
        SubscribeLocalEvent<ModStatusReadoutComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ModStatusReadoutComponent, ModModuleRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private bool CanRead(EntityUid module, EntityUid user)
    {
        return _suits.TryGetInstalledModuleSuit(module, out var suit)
            && (suit.Comp.Wearer == user || _suits.IsAssistant(suit, user)) && suit.Comp.State == ModSuitState.Active
            && suit.Comp.Powered && !suit.Comp.Projected;
    }

    private void OnUsed(Entity<ModStatusReadoutComponent> ent, ref ModModuleUsedEvent args)
    {
        var actor = args.Operator ?? args.User;
        if (!CanRead(ent, actor))
            return;

        args.Handled = _ui.TryToggleUi(ent.Owner, ModStatusReadoutUiKey.Key, actor);
    }

    private void OnOpen(Entity<ModStatusReadoutComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!args.UiKey.Equals(ModStatusReadoutUiKey.Key))
            return;

        if (!CanRead(ent, args.Actor))
        {
            _ui.CloseUi(ent.Owner, ModStatusReadoutUiKey.Key, args.Actor);
            return;
        }

        _open.Add(ent);
        Refresh(ent);
    }

    private void OnClose(Entity<ModStatusReadoutComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!_ui.IsUiOpen(ent.Owner, ModStatusReadoutUiKey.Key))
            _open.Remove(ent);
    }

    private void OnShutdown(Entity<ModStatusReadoutComponent> ent, ref ComponentShutdown args)
        => _open.Remove(ent);

    private void OnRemoved(Entity<ModStatusReadoutComponent> ent, ref ModModuleRemovedEvent args)
        => _ui.CloseUi(ent.Owner, ModStatusReadoutUiKey.Key);

    public override void Update(float frameTime)
    {
        if (_open.Count == 0)
            return;
        _untilRefresh -= frameTime;
        if (_untilRefresh > 0f)
            return;
        _untilRefresh = 1f;

        foreach (var uid in _open.ToArray())
        {
            if (TryComp<ModStatusReadoutComponent>(uid, out var readout))
                Refresh((uid, readout));
        }
    }

    private void Refresh(Entity<ModStatusReadoutComponent> ent)
    {
        if (!_suits.TryGetInstalledModuleSuit(ent, out var suit) || suit.Comp.Wearer is not { } wearer
            || GetReadout(ent, wearer) is not { } state
            || !_ui.IsUiOpen(ent.Owner, ModStatusReadoutUiKey.Key))
        {
            _ui.CloseUi(ent.Owner, ModStatusReadoutUiKey.Key);
            _open.Remove(ent);
            return;
        }

        _ui.SetUiState(ent.Owner, ModStatusReadoutUiKey.Key, state);
    }

    public ModStatusReadoutState? GetReadout(Entity<ModStatusReadoutComponent> module, EntityUid user)
    {
        if (!CanRead(module, user))
            return null;

        if (!_suits.TryGetInstalledModuleSuit(module, out var suit) || suit.Comp.Wearer is not { } wearer)
            return null;
        user = wearer;

        return new ModStatusReadoutState(
            MetaData(user).EntityName,
            CompOrNull<MobStateComponent>(user)?.CurrentState ?? MobState.Invalid,
            (float) _damage.GetTotalDamage(user),
            CompOrNull<TemperatureComponent>(user)?.Temperature,
            HasComp<BloodstreamComponent>(user) ? _blood.GetBloodLevel(user) : null,
            CompOrNull<StaminaComponent>(user)?.StaminaDamage,
            module.Comp.ShowFingerprint ? CompOrNull<FingerprintComponent>(user)?.Fingerprint : null);
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead || args.OldMobState == MobState.Dead)
            return;

        var query = EntityQueryEnumerator<ModStatusReadoutComponent>();
        while (query.MoveNext(out var uid, out var readout))
        {
            if (CanRead(uid, args.Target))
                _audio.PlayPvs(readout.DeathSound, args.Target);
        }
    }
}
