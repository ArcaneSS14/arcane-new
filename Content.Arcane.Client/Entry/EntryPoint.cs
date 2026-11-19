using Robust.Shared.ContentPack;
using Content.Arcane.Client.JoinQueue;

namespace Content.Arcane.Client.Entry;

public sealed class EntryPoint : GameClient
{
    private JoinQueueManager? _queue;

    public override void PreInit()
    {
        Dependencies.Register<JoinQueueManager>();
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
