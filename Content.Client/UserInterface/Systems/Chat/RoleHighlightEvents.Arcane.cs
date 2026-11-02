// Arcane-Start
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Systems.Chat;

/// <summary>
///     Requests the owner-specific editor for a job's automatic chat highlights.
/// </summary>
[ByRefEvent]
public readonly record struct OpenRoleHighlightEditorEvent(ProtoId<JobPrototype> job)
{
    public ProtoId<JobPrototype> Job { get; } = job;
}

/// <summary>
///     Allows a client module to provide saved automatic highlights for a job.
/// </summary>
[ByRefEvent]
public record struct GetRoleHighlightsEvent(ProtoId<JobPrototype> job)
{
    public ProtoId<JobPrototype> Job { get; } = job;
    public string? Highlights { get; set; }
}
// Arcane-End
