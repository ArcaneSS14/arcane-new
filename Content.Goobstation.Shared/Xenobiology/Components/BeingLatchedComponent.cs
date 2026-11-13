// SPDX-License-Identifier: AGPL-3.0-or-later


using Robust.Shared.GameStates; // Arcane

namespace Content.Goobstation.Shared.Xenobiology.Components;

/// <summary>
/// Marks an entity as being consumed so it is not targeted by other entities.
/// Freaky.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState] // Arcane-Edit: retain the owner of this reservation
public sealed partial class BeingLatchedComponent : Component
{
    // Arcane-Start
    [AutoNetworkedField]
    public EntityUid? Slime;
    // Arcane-End
}
