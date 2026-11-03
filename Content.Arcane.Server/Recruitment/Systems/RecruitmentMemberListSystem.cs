using Content.Arcane.Shared.Recruitment;
using Content.Arcane.Shared.Recruitment.Components;
using Robust.Server.GameObjects;

namespace Content.Arcane.Server.Recruitment.Systems;

public sealed partial class RecruitmentMemberListSystem : EntitySystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;

    private const float UpdateIntervalSeconds = 2f;
    private float _nextUpdateAt;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RecruitmentScanningComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
    }

    private void OnBoundUIOpened(EntityUid uid, RecruitmentScanningComponent comp, BoundUIOpenedEvent args)
    {
        if (args.UiKey is not RecruitmentMemberListUiKey)
            return;

        UpdateMemberList(uid, comp);
    }

    private void UpdateMemberList(EntityUid uid, RecruitmentScanningComponent comp)
    {
        _ui.SetUiState(uid, RecruitmentMemberListUiKey.Key, BuildMemberList(comp.OrganizationName));
    }

    /// <summary>
    /// The member list of one organization; built once per refresh and shared by all its open scanners.
    /// </summary>
    private RecruitmentMemberListBuiState BuildMemberList(string organization)
    {
        var members = new List<RecruitmentMemberListBuiState.RecruitedMemberData>();

        // Query all recruited entities for this organization
        var query = AllEntityQuery<RecruitedComponent, MetaDataComponent>();
        while (query.MoveNext(out _, out var recruited, out var meta))
        {
            if (recruited.Organization != organization)
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

        return new RecruitmentMemberListBuiState(organization, members.ToArray());
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _nextUpdateAt -= frameTime;
        if (_nextUpdateAt > 0f)
            return;

        _nextUpdateAt = UpdateIntervalSeconds;

        var states = new Dictionary<string, RecruitmentMemberListBuiState>();
        var query = EntityQueryEnumerator<RecruitmentScanningComponent>();
        while (query.MoveNext(out var scannerUid, out var scannerComp))
        {
            if (!_ui.IsUiOpen(scannerUid, RecruitmentMemberListUiKey.Key))
                continue;

            if (!states.TryGetValue(scannerComp.OrganizationName, out var state))
            {
                state = BuildMemberList(scannerComp.OrganizationName);
                states[scannerComp.OrganizationName] = state;
            }

            _ui.SetUiState(scannerUid, RecruitmentMemberListUiKey.Key, state);
        }
    }
}
