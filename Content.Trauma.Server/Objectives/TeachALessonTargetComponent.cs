// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.Objectives;

/// <summary>
/// Tracks objectives targeting this mind and the character body being watched.
/// </summary>

[RegisterComponent ]
public sealed partial class TeachALessonTargetComponent : Component
{
    [DataField]
    public HashSet<EntityUid> Teachers = new HashSet<EntityUid>();

    [DataField]
    public EntityUid? Body;
}

/// <summary>Targets whose actual character body is this entity.</summary>
[RegisterComponent]
public sealed partial class TeachALessonBodyComponent : Component
{
    [DataField]
    public HashSet<EntityUid> Minds = new();
}
