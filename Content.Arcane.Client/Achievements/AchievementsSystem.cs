using Content.Arcane.Shared.Achievements;
using Robust.Shared.Network;

namespace Content.Arcane.Client.Achievements;

public sealed partial class AchievementsSystem : EntitySystem
{
    [Dependency] private IClientNetManager _net = default!;

    public List<AchievementData>? Achievements { get; private set; }

    public event Action? Updated;
    public event Action<AchievementData>? Unlocked;
    public event Action? Reset;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AchievementsStateEvent>(OnState);
        SubscribeNetworkEvent<AchievementUnlockedEvent>(OnUnlocked);
        _net.Disconnect += OnDisconnect;
    }

    public override void Shutdown()
    {
        _net.Disconnect -= OnDisconnect;
        Achievements = null;
        base.Shutdown();
    }

    public void RequestState()
    {
        if (_net.ServerChannel == null)
            return;

        RaiseNetworkEvent(new RequestAchievementsEvent());
    }

    private void OnDisconnect(object? sender, NetDisconnectedArgs args)
    {
        Achievements = null;
        Reset?.Invoke();
    }

    private void OnState(AchievementsStateEvent ev)
    {
        Achievements = ev.Achievements;
        Updated?.Invoke();
    }

    private void OnUnlocked(AchievementUnlockedEvent ev)
    {
        if (Achievements != null)
        {
            var index = Achievements.FindIndex(a => a.Id == ev.Achievement.Id);
            if (index >= 0)
                Achievements[index] = ev.Achievement;
            else
                Achievements.Add(ev.Achievement);
        }

        Unlocked?.Invoke(ev.Achievement);
        Updated?.Invoke();
    }
}
