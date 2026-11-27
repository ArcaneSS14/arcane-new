using Robust.Shared.Configuration;

namespace Content.Arcane.Common.CCVar;

public sealed partial class ArcaneCVars
{
    /// <summary>
    ///     Custom species names cannot be the name of another species.
    /// </summary>
    public static readonly CVarDef<bool> RestrictedCustomSpeciesNames =
        CVarDef.Create("ic.restricted_custom_species_names", true, CVar.SERVER | CVar.REPLICATED);
}
