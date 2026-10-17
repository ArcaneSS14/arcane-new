using Robust.Shared.Configuration;

namespace Content.Arcane.Common.CCVar;

public sealed partial class ArcaneCVars
{
    /// <summary>
    /// Plays ear ringing when the local character's concussion worsens.
    /// </summary>
    public static readonly CVarDef<bool> ConcussionSound =
        CVarDef.Create("arcane.concussion_sound", true, CVar.CLIENTONLY | CVar.ARCHIVE);
}
