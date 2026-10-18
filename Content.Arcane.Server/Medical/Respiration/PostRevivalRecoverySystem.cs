using Content.Shared.Mobs;
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
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            RemoveOwnEffect(args.Target, RecoverySleep);
            RemoveOwnEffect(args.Target, RecoveryDrowsiness);
            return;
        }

        if (args.OldMobState != MobState.Dead)
            return;

        RemoveOwnEffect(args.Target, RecoverySleep);
        RemoveOwnEffect(args.Target, RecoveryDrowsiness);
        var unconscious = TimeSpan.FromSeconds(5);
        _status.TrySetStatusEffectDuration(args.Target, RecoverySleep, unconscious);
        _status.TrySetStatusEffectDuration(args.Target, RecoveryDrowsiness, TimeSpan.FromSeconds(30), unconscious);
    }

    private void RemoveOwnEffect(EntityUid patient, EntProtoId prototype)
    {
        // Remove our entity synchronously so same-tick revival cannot refresh an effect queued for deletion.
        if (_status.TryGetStatusEffect(patient, prototype, out var effect))
            Del(effect.Value);
    }
}
