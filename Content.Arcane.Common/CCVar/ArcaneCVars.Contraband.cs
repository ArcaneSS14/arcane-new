using Robust.Shared.Configuration;

namespace Content.Arcane.Common.CCVar;

public sealed partial class ArcaneCVars
{
    public static readonly CVarDef<bool> ContrabandIconsEnabled =
        CVarDef.Create("contraband.icons_enabled", false, CVar.SERVER | CVar.REPLICATED);
}
