using Robust.Shared.Configuration;

namespace Content.Arcane.Common.CCVar;

public sealed partial class ArcaneCVars
{
    /// <summary>
    /// Plays ear ringing throughout the local character's concussion and a sound on recovery.
    /// </summary>
    public static readonly CVarDef<bool> ConcussionSound =
        CVarDef.Create("arcane.concussion_sound", true, CVar.CLIENTONLY | CVar.ARCHIVE);
}
