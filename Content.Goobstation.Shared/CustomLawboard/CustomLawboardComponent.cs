// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Silicons.Laws;

using Robust.Shared.GameStates; // Arcane

namespace Content.Goobstation.Shared.CustomLawboard;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState] // Arcane-Edit: Synchronize lawboard contents
public sealed partial class CustomLawboardComponent : Component
{
    /// <summary>
    /// The laws of this custom lawboard.
    /// </summary>
    [DataField, AutoNetworkedField] // Arcane
    public List<SiliconLaw> Laws = new();
}
