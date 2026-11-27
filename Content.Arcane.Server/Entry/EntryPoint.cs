using Content.Arcane.Server.Achievements;
using Content.Arcane.Server.IoC;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Arcane.Server.Entry;

public sealed class EntryPoint : GameServer
{
    public override void PreInit()
    {
        ServerArcaneContentIoC.Register(Dependencies);
    }

    public override void Init()
    {
        base.Init();

        Dependencies.BuildGraph();
    }

    public override void Shutdown()
    {
        // Normal server cleanup clears entity systems without calling their Shutdown methods.
        if (IoCManager.Resolve<IEntitySystemManager>().TryGetEntitySystem<AchievementManager>(out var achievements))
            achievements.FlushForShutdown();
        base.Shutdown();
    }
}
