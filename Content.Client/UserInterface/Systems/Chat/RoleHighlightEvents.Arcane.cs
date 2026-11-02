// Arcane-Start
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Systems.Chat;

/// <summary>
///     Requests the owner-specific editor for a job's automatic chat highlights.
/// </summary>
public sealed class OpenRoleHighlightEditorEvent(ProtoId<JobPrototype> job) : EntityEventArgs
{
    public ProtoId<JobPrototype> Job { get; } = job;
}

/// <summary>
///     Allows a client module to provide saved automatic highlights for a job.
/// </summary>
public sealed class GetRoleHighlightsEvent(ProtoId<JobPrototype> job) : EntityEventArgs
{
    public ProtoId<JobPrototype> Job { get; } = job;
    public string? Highlights { get; set; }
}
// Arcane-End
