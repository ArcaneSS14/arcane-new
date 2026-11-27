using Robust.Shared.Configuration;

namespace Content.Arcane.Common.CCVar;

public sealed partial class ArcaneCVars
{
    /// <summary>
    /// UI colour theme: "nanotrasen" for the stock look, or an Arcane theme id.
    /// </summary>
    public static readonly CVarDef<string> UiTheme =
        CVarDef.Create("ui.arcane_theme", "nanotrasen", CVar.CLIENTONLY | CVar.ARCHIVE);
}
