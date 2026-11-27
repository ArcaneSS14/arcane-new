// Arcane-Start
using Content.Arcane.Common.CCVar;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    /// <summary>
    /// Alternate title picked for each job, if any.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<JobPrototype>, ProtoId<JobAlternateTitlePrototype>> JobAlternateTitles { get; private set; } = new();

    public HumanoidCharacterProfile WithJobAltTitle(ProtoId<JobPrototype> job, ProtoId<JobAlternateTitlePrototype>? title)
    {
        var titles = new Dictionary<ProtoId<JobPrototype>, ProtoId<JobAlternateTitlePrototype>>(JobAlternateTitles);
        if (title is { } picked)
            titles[job] = picked;
        else
            titles.Remove(job);

        return new(this) { JobAlternateTitles = titles };
    }

    public HumanoidCharacterProfile WithJobAltTitles(Dictionary<ProtoId<JobPrototype>, ProtoId<JobAlternateTitlePrototype>> titles)
    {
        return new(this) { JobAlternateTitles = new(titles) };
    }

    private bool AltTitlesEqual(HumanoidCharacterProfile other)
    {
        if (JobAlternateTitles.Count != other.JobAlternateTitles.Count)
            return false;

        foreach (var (job, title) in JobAlternateTitles)
        {
            if (!other.JobAlternateTitles.TryGetValue(job, out var otherTitle) || otherTitle != title)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Keeps the titles that belong to their job and whose requirements the player meets.
    /// </summary>
    private Dictionary<ProtoId<JobPrototype>, ProtoId<JobAlternateTitlePrototype>> ValidatedAltTitles(
        ICommonSession session,
        IDependencyCollection collection,
        IPrototypeManager prototypes,
        IConfigurationManager cfg)
    {
        var titles = new Dictionary<ProtoId<JobPrototype>, ProtoId<JobAlternateTitlePrototype>>();
        if (!cfg.GetCVar(ArcaneCVars.AlternateJobTitlesEnabled))
            return titles;

        IReadOnlyDictionary<string, TimeSpan>? playTimes = null;
        var entities = collection.Resolve<IEntityManager>();
        foreach (var (jobId, titleId) in JobAlternateTitles)
        {
            if (!prototypes.TryIndex(jobId, out var job)
                || !job.AlternateTitles.Contains(titleId)
                || !prototypes.TryIndex(titleId, out var title))
                continue;

            if (title.Requirements != null)
            {
                playTimes ??= collection.Resolve<ISharedPlaytimeManager>().GetPlayTimes(session);
                if (!JobRequirements.TryRequirementsMet(title.Requirements, playTimes, out _, entities, prototypes, this))
                    continue;
            }

            titles.Add(jobId, titleId);
        }

        return titles;
    }
}
// Arcane-End
