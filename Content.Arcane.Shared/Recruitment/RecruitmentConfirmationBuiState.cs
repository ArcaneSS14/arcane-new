using Robust.Shared.Serialization;

namespace Content.Arcane.Shared.Recruitment;

/// <summary>
/// What the recruitment confirmation window shows.
/// </summary>
[Serializable, NetSerializable]
public sealed class RecruitmentConfirmationBuiState
{
    public string OrganizationName = string.Empty;
    public string ImplantName = string.Empty;
}
