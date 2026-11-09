using Robust.Shared.Serialization;

namespace Content.Arcane.Shared.Recruitment.Events;

[Serializable, NetSerializable]
public sealed class RecruitmentOpenConfirmationEvent : EntityEventArgs
{
    public NetEntity Scanner { get; init; }
    public string OrganizationName { get; init; } = string.Empty;
    public string ImplantName { get; init; } = string.Empty;
}

/// <summary>
/// Closes the target's confirmation window: the request was answered, replaced or its scanner is gone.
/// </summary>
[Serializable, NetSerializable]
public sealed class RecruitmentCloseConfirmationEvent : EntityEventArgs
{
    public NetEntity Scanner { get; init; }
}

[Serializable, NetSerializable]
public sealed class RecruitmentRespondConfirmationEvent : EntityEventArgs
{
    public NetEntity Scanner { get; init; }
    public bool Accepted { get; init; }
}
