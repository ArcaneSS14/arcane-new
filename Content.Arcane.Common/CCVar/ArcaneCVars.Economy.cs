using Robust.Shared.Configuration;

namespace Content.Arcane.Common.CCVar;

public sealed partial class ArcaneCVars
{
    /// <summary>
    ///     Minutes between two paydays.
    /// </summary>
    public static readonly CVarDef<float> PaydayInterval =
        CVarDef.Create("economy.payday_interval", 5f, CVar.SERVERONLY);

    /// <summary>
    ///     Personal cargo orders cost this much more, the difference goes to the ordering account.
    /// </summary>
    public static readonly CVarDef<float> PrivateOrderMarkup =
        CVarDef.Create("economy.private_order_markup", 1.1f, CVar.SERVER | CVar.REPLICATED);
}
