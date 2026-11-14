using Content.Arcane.Common.CCVar;
using Content.Server.Chat.Managers;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Chat;
using Content.Shared.Cloning.Events;
using Content.Shared.GameTicking;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.Roles;

/// <summary>
/// Puts the alternate job title a player picked on their ID card and station record, reminds them that it is
/// only a title, and carries it over to the ID card of their clone.
/// </summary>
public sealed partial class AltTitleSystem : EntitySystem
{
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;
    [Dependency] private StationRecordsSystem _records = default!;

    public override void Initialize()
    {
        base.Initialize();

        // After the records system, so the general record exists and its job title can be replaced.
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn, after: [typeof(StationRecordsSystem)]);
        SubscribeLocalEvent<PdaComponent, CloningItemEvent>(OnClonePda);
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent ev)
    {
        if (!_cfg.GetCVar(ArcaneCVars.AlternateJobTitlesEnabled)
            || ev.JobId is not { } jobId
            || !_prototypes.TryIndex<JobPrototype>(jobId, out var job)
            || !ev.Profile.JobAlternateTitles.TryGetValue(job.ID, out var titleId)
            || !_prototypes.TryIndex(titleId, out var title))
            return;

        var name = title.LocalizedName(ev.Profile.Gender);
        if (_idCard.TryFindIdCard(ev.Mob, out var card))
        {
            _idCard.TryChangeJobTitle(card, name, card.Comp);

            if (TryComp<StationRecordKeyStorageComponent>(card, out var keyStorage)
                && keyStorage.Key is { } key
                && _records.TryGetRecord<GeneralStationRecord>(key, out var record))
            {
                record.JobTitle = name;
                _records.Synchronize(key);
            }
        }

        var message = Loc.GetString("job-alt-title-reminder", ("altTitle", name), ("jobName", job.LocalizedName));
        var wrapped = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
        _chat.ChatMessageToOne(ChatChannel.Server, message, wrapped, default, false, ev.Player.Channel);
    }

    private void OnClonePda(Entity<PdaComponent> original, ref CloningItemEvent args)
    {
        // The clone's PDA spawns a fresh ID card from its prototype, which has the job's own title.
        if (original.Comp.ContainedId is not { } originalId
            || !TryComp<IdCardComponent>(originalId, out var card)
            || string.IsNullOrEmpty(card.LocalizedJobTitle)
            || !TryComp<PdaComponent>(args.CloneUid, out var clonePda)
            || clonePda.ContainedId is not { } cloneId)
            return;

        _idCard.TryChangeJobTitle(cloneId, card.LocalizedJobTitle);
    }
}
