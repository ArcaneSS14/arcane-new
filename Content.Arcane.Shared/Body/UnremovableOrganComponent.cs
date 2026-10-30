// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Arcane.Shared.Body;

/// <summary>
/// Marks an organ as impossible to remove from its body through surgery.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class UnremovableOrganComponent : Component;
