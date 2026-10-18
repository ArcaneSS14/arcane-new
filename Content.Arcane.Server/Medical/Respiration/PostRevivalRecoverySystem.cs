using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.Medical.Respiration;

/// <summary>
/// Gives every actual revival the same short unconscious recovery and subsequent visual drowsiness.
/// </summary>
public sealed partial class PostRevivalRecoverySystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _status = default!;

    public static readonly EntProtoId RecoverySleep = "StatusEffectResuscitationSleep";
    public static readonly EntProtoId RecoveryDrowsiness = "StatusEffectResuscitationDrowsiness";

    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<MobStateComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            RemoveOwnEffect(ent, RecoverySleep);
            RemoveOwnEffect(ent, RecoveryDrowsiness);
            return;
        }

        if (args.OldMobState != MobState.Dead)
            return;

        RemoveOwnEffect(ent, RecoverySleep);
        RemoveOwnEffect(ent, RecoveryDrowsiness);
        var unconscious = TimeSpan.FromSeconds(5);
        _status.TrySetStatusEffectDuration(ent, RecoverySleep, unconscious);
        _status.TrySetStatusEffectDuration(ent, RecoveryDrowsiness, TimeSpan.FromSeconds(30), unconscious);
    }

    private void RemoveOwnEffect(EntityUid patient, EntProtoId prototype)
    {
        // Remove our entity synchronously so same-tick revival cannot refresh an effect queued for deletion.
        if (_status.TryGetStatusEffect(patient, prototype, out var effect))
            Del(effect.Value);
    }
}
