using Content.Shared.Whitelist;

namespace Content.Goobstation.Shared.Clothing.Components;

/// <summary>
/// Clothing that can't take this coating (Orion #437).
/// </summary>
public sealed partial class ClothingCoatingComponent
{
    [DataField]
    public EntityWhitelist? Blacklist;
}
