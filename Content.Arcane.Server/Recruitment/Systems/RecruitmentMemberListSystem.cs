using Content.Arcane.Shared.Recruitment;
using Content.Arcane.Shared.Recruitment.Components;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;

namespace Content.Arcane.Server.Recruitment.Systems;

public sealed partial class RecruitmentMemberListSystem : EntitySystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RecruitmentScanningComponent, AfterActivatableUIOpenEvent>(OnUIOpen);
        SubscribeLocalEvent<RecruitmentScanningComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
    }

    private void OnUIOpen(EntityUid uid, RecruitmentScanningComponent comp, AfterActivatableUIOpenEvent args)
    {
        UpdateMemberList(uid, comp);
    }

    private void OnBoundUIOpened(EntityUid uid, RecruitmentScanningComponent comp, BoundUIOpenedEvent args)
    {
        if (args.UiKey is not RecruitmentMemberListUiKey)
            return;

        UpdateMemberList(uid, comp);
    }

    private void UpdateMemberList(EntityUid uid, RecruitmentScanningComponent comp)
    {
        var members = new List<RecruitmentMemberListBuiState.RecruitedMemberData>();

        // Query all recruited entities for this organization
        var query = AllEntityQuery<RecruitedComponent, MetaDataComponent>();
        while (query.MoveNext(out _, out var recruited, out var meta))
        {
            if (recruited.Organization != comp.OrganizationName)
                continue;

            var memberName = meta.EntityName;
            var recruiterName = string.IsNullOrWhiteSpace(recruited.RecruitedBy)
                ? Loc.GetString("recruitment-member-list-unknown")
                : recruited.RecruitedBy!;

            members.Add(new RecruitmentMemberListBuiState.RecruitedMemberData(
                memberName,
                recruiterName,
                recruited.RecruitedAt
            ));
        }

        members.Sort((a, b) => b.RecruitedAt.CompareTo(a.RecruitedAt));

        var state = new RecruitmentMemberListBuiState(comp.OrganizationName, members.ToArray());
        _ui.SetUiState(uid, RecruitmentMemberListUiKey.Key, state);
    }
}
