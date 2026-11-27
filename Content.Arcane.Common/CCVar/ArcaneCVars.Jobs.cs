using Robust.Shared.Configuration;

namespace Content.Arcane.Common.CCVar;

public sealed partial class ArcaneCVars
{
    /// <summary>
    /// Lets players pick an alternate title for a job; it goes on their ID card and station record.
    /// </summary>
    public static readonly CVarDef<bool> AlternateJobTitlesEnabled =
        CVarDef.Create("ic.alternate_job_titles_enable", true, CVar.SERVER | CVar.REPLICATED);
}
