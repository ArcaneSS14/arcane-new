using Content.Server.GameTicking;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.GameTicking;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Network;

namespace Content.Arcane.Server.Achievements.Conditions;

public sealed partial class UndamagedCondition : AchievementCondition;

public sealed partial class UndamagedConditionSystem : AchievementConditionSystem<UndamagedCondition>
{
    [Dependency] private IPlayerManager _player = default!;

    private readonly Dictionary<EntityUid, NetUserId> _eligible = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawned);
        SubscribeLocalEvent<DamageableComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnd);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnSpawned(PlayerSpawnCompleteEvent ev)
    {
        if (!ev.LateJoin && Achievements.TryGetSession(ev.Mob, out var session))
            _eligible[ev.Mob] = session.UserId;
    }

    private void OnDamageChanged(Entity<DamageableComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageIncreased)
            _eligible.Remove(ent);
    }

    private void OnRoundEnd(ref RoundEndTextAppendEvent ev)
    {
        foreach (var session in _player.Sessions)
        {
            if (session.AttachedEntity is { } player && _eligible.TryGetValue(player, out var owner)
                && owner == session.UserId)
                ProgressAll(player);
        }
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _eligible.Clear();
    }
}
