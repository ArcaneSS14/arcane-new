// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Silicons.Laws;
using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.CustomLawboard;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState] // Arcane
public sealed partial class CustomLawboardComponent : Component
{
    /// <summary>
    /// The laws of this custom lawboard.
    /// </summary>
    [DataField, AutoNetworkedField] // Arcane
    public List<SiliconLaw> Laws = new();
}
