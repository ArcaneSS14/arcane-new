// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Arcane.Shared.Power;

/// <summary>
/// A large cell for machines like SMES. Only fits power cell slots marked with <see cref="MegaCellSlotComponent"/> (Orion #192, #323).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MegaCellComponent : Component;

/// <summary>
/// Lets this entity's power cell slot accept mega cells.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MegaCellSlotComponent : Component;
