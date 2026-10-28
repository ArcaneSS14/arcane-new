// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Arcane.Server.Ghost;

/// <summary>
/// The last mind that left this body, so the ghost warp menu can still show a dead body's job.
/// </summary>
[RegisterComponent]
public sealed partial class LastMindComponent : Component
{
    [ViewVariables]
    public EntityUid? Mind;
}
