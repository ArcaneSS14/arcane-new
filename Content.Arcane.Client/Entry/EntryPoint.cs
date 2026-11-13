using Robust.Shared.ContentPack;
using Content.Arcane.Client.JoinQueue;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Client.Entry;

public sealed class EntryPoint : GameClient
{
    private JoinQueueManager? _queue;

    public override void PreInit()
    {
        Dependencies.Register<JoinQueueManager>();
    }

    public override void Init()
    {
        base.Init();
        Dependencies.Resolve<IPrototypeManager>().RegisterIgnore("achievement");
    }

    public override void PostInit()
    {
        base.PostInit();
        _queue = Dependencies.Resolve<JoinQueueManager>();
        _queue.Initialize();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _queue?.Shutdown();
        base.Dispose(disposing);
    }
}
