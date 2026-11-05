namespace Content.Arcane.Shared.Recruitment.Components;

[RegisterComponent]
public sealed partial class RecruitedComponent : Component
{
    [DataField]
    public string Organization = string.Empty;

    [DataField]
    public string? RecruitedBy = string.Empty;

    [DataField]
    public TimeSpan RecruitedAt;
}
