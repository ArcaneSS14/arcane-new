// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Arcane.Common.Preferences;

namespace Content.Shared.Preferences;

/// <summary>
/// Arcane - extended flavor text (Orion #165, #183).
/// </summary>
public sealed partial class HumanoidCharacterProfile
{
    /// <summary>
    /// Additional description sections, shown in the detail examine window.
    /// </summary>
    [DataField]
    public ExtendedFlavorText ExtendedFlavor { get; private set; } = new();

    public HumanoidCharacterProfile WithExtendedFlavor(ExtendedFlavorText flavor)
    {
        return new(this) { ExtendedFlavor = flavor };
    }
}
