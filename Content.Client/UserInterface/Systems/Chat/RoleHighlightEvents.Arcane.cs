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

public static class RoleHighlightDefaults
{
    public static string GetKey(ProtoId<JobPrototype> job) => $"highlights-{job.Id.Replace(' ', '-').ToLowerInvariant()}";

    public static string Normalize(string value) => string.Join(
        '\n', value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
// Arcane-End
