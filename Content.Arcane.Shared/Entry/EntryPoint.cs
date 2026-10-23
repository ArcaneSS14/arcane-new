using Robust.Shared.ContentPack;

namespace Content.Arcane.Shared.Entry;

public sealed partial class EntryPoint : GameShared
{
    [Dependency] private IPrototypeManager _prototypes = default!;

    public override void PreInit()
    {
        Dependencies.InjectDependencies(this);
    }

    public override void Init()
    {
        _prototypes.PartialDirectory(new("/Prototypes/_Arcane/Partials"), 2);
    }
}
