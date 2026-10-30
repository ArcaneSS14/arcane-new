// Arcane-Start
using Content.Shared.Whitelist;

namespace Content.Goobstation.Shared.Clothing.Components;

/// <summary>
/// Arcane - clothing that can't take this coating (Orion #437).
/// </summary>
public sealed partial class ClothingCoatingComponent
{
    [DataField]
    public EntityWhitelist? Blacklist;
}
// Arcane-End
