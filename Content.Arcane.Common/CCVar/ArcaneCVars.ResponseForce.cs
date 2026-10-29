using Robust.Shared.Configuration;

namespace Content.Arcane.Common.CCVar;

public sealed partial class ArcaneCVars
{
    /// <summary>
    ///     Minutes that have to pass between two response force calls.
    /// </summary>
    public static readonly CVarDef<int> ResponseForceDelay =
        CVarDef.Create("responseforce.delay", 0, CVar.SERVERONLY);
}
