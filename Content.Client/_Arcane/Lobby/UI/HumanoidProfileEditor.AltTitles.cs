// Arcane-Start
using Content.Arcane.Common.CCVar;
using Content.Client.Lobby.UI.Roles;
using Content.Shared.Roles;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI;

/// <summary>
/// Alternate job titles in the job list: a job that has them gets a dropdown in place of its name.
/// </summary>
public sealed partial class HumanoidProfileEditor
{
    private void SetupJobAltTitle(RequirementsSelector selector, JobPrototype job)
    {
        if (job.AlternateTitles.Count == 0
            || !IoCManager.Resolve<IConfigurationManager>().GetCVar(ArcaneCVars.AlternateJobTitlesEnabled))
            return;

        Label? title = null;
        foreach (var child in selector.Children)
        {
            if (child is Label label)
            {
                title = label;
                break;
            }
        }

        if (title == null)
            return;

        var titles = new List<ProtoId<JobAlternateTitlePrototype>>();
        var options = new OptionButton
        {
            SetWidth = title.SetWidth,
            Margin = title.Margin,
            ToolTip = title.ToolTip,
        };
        options.AddItem(job.LocalizedName, 0);
        foreach (var id in job.AlternateTitles)
        {
            if (!_prototypeManager.TryIndex(id, out var alt))
                continue;

            titles.Add(id);
            options.AddItem(alt.LocalizedName(Profile?.Gender), titles.Count);
        }

        if (Profile?.JobAlternateTitles.TryGetValue(job.ID, out var current) == true && titles.IndexOf(current) is var index and >= 0)
            options.SelectId(index + 1);

        options.OnItemSelected += args =>
        {
            foreach (var (jobId, other) in _jobPriorities)
            {
                if (jobId != job.ID)
                    continue;

                foreach (var child in other.Children)
                {
                    if (child is OptionButton titlesForJob)
                        titlesForJob.TrySelectId(args.Id);
                }
            }
            Profile = Profile?.WithJobAltTitle(job.ID, args.Id == 0 ? null : (ProtoId<JobAlternateTitlePrototype>?) titles[args.Id - 1]);
            SetDirty();
        };

        selector.AddChild(options);
        options.SetPositionInParent(title.GetPositionInParent());
        title.Visible = false;
    }
}
// Arcane-End
