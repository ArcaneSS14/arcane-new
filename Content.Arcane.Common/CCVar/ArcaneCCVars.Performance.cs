using Robust.Shared.Configuration;

namespace Content.Arcane.Common.CCVar;

public sealed partial class ArcaneCCVars
{
    /// <summary>
    /// Compiles the serialization delegates of entity components in the background after the client has loaded,
    /// so that the first entity of each kind doesn't cause a stutter in game.
    /// </summary>
    public static readonly CVarDef<bool> ComponentCopyWarmup =
        CVarDef.Create("content.component_copy_warmup", true, CVar.CLIENTONLY);
}
