using Robust.Shared.Configuration;

namespace Content.Arcane.Common.Thunderdome;

[CVarDefs]
public sealed class ThunderdomeCVars
{
    public static readonly CVarDef<bool> Enabled = CVarDef.Create("thunderdome.enabled", true, CVar.SERVER | CVar.REPLICATED);
    public static readonly CVarDef<bool> Refill = CVarDef.Create("thunderdome.refill", true, CVar.SERVERONLY);
}
