using System.Diagnostics.CodeAnalysis;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Shared.CustomGhost;


// omitting CustomGhost prefix for convenience when prototyping
[DataDefinition]
public sealed partial class CkeyRestriction : CustomGhostRestriction
{
    [DataField(required: true)]
    public List<string> Ckey = new();

    public override bool HideOnFail => true;

    public override bool CanUse(ICommonSession player, [NotNullWhen(false)] out string? failReason)
    {
        failReason = null;

        // Local dev logins are "localhost@<name>"; the ckey is the part after the prefix.
        const string localPrefix = "localhost@";
        var name = player.Name.StartsWith(localPrefix, StringComparison.Ordinal)
            ? player.Name[localPrefix.Length..]
            : player.Name;

        // Ckeys are case insensitive; ordinal comparison does not depend on the current culture.
        foreach (var testCkey in Ckey)
        {
            if (string.Equals(testCkey, name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        failReason = Loc.GetString("custom-ghost-fail-exclusive-ghost");
        return false;
    }
}



[DataDefinition]
public sealed partial class PlaytimeServerRestriction : CustomGhostRestriction
{

    [DataField(required: true)]
    public float HoursPlaytime;

    public override bool CanUse(ICommonSession player, [NotNullWhen(false)] out string? failReason)
    {
        var playtime = IoCManager.Resolve<ISharedPlaytimeManager>();

        failReason = null;
        var playtimes = playtime.GetPlayTimes(player);

        double jobPlaytime = 0;
        if (playtimes.TryGetValue("Overall", out var time))
            jobPlaytime += time.TotalHours;

        if (jobPlaytime < HoursPlaytime)
        {
            failReason = Loc.GetString("custom-ghost-fail-server-insufficient-playtime",
                    ("requiredHours", MathF.Round(HoursPlaytime)),
                    ("requiredMinutes", MathF.Round(HoursPlaytime % 1 * 60)),
                    ("playtimeHours", Math.Round(jobPlaytime)),
                    ("playtimeMinutes", Math.Round(jobPlaytime % 1 * 60))
            );
            return false;
        }

        return true;
    }
}


[DataDefinition]
public sealed partial class PlaytimeJobRestriction : CustomGhostRestriction
{

    [DataField(required: true)]
    public ProtoId<JobPrototype> Job = string.Empty;

    [DataField(required: true)]
    public float HoursPlaytime;

    public override bool CanUse(ICommonSession player, [NotNullWhen(false)] out string? failReason)
    {
        var playtime = IoCManager.Resolve<ISharedPlaytimeManager>();
        var proto = IoCManager.Resolve<IPrototypeManager>();

        failReason = null;
        var playtimes = playtime.GetPlayTimes(player);

        double jobPlaytime = 0;
        var jobProto = proto.Index(Job);
        if (playtimes.TryGetValue(jobProto.PlayTimeTracker, out var time))
            jobPlaytime += time.TotalHours;

        if (jobPlaytime < HoursPlaytime)
        {
            failReason = Loc.GetString("custom-ghost-fail-job-insufficient-playtime",
                    ("job", Loc.GetString(jobProto.Name)),
                    ("requiredHours", MathF.Round(HoursPlaytime)),
                    ("requiredMinutes", MathF.Round(HoursPlaytime % 1 * 60)),
                    ("playtimeHours", Math.Round(jobPlaytime)),
                    ("playtimeMinutes", Math.Round(jobPlaytime % 1 * 60))
            );
            return false;
        }

        return true;
    }
}


[DataDefinition]
public sealed partial class PlaytimeDepartmentRestriction : CustomGhostRestriction
{

    [DataField(required: true)]
    public ProtoId<DepartmentPrototype> Department = string.Empty;

    [DataField(required: true)]
    public float HoursPlaytime;

    public override bool CanUse(ICommonSession player, [NotNullWhen(false)] out string? failReason)
    {
        var playtime = IoCManager.Resolve<ISharedPlaytimeManager>();
        var proto = IoCManager.Resolve<IPrototypeManager>();

        failReason = null;
        var playtimes = playtime.GetPlayTimes(player);

        double departmentPlaytime = 0;
        var departmentProto = proto.Index(Department);
        var departmentJobs = departmentProto.Roles;
        foreach (var job in departmentJobs)
        {
            var jobProto = proto.Index(job);
            if (playtimes.TryGetValue(jobProto.PlayTimeTracker, out var time))
                departmentPlaytime += time.TotalHours;
        }

        if (departmentPlaytime < HoursPlaytime)
        {
            failReason = Loc.GetString("custom-ghost-fail-department-insufficient-playtime",
                    ("department", Loc.GetString(departmentProto.Name)),
                    ("requiredHours", MathF.Round(HoursPlaytime)),
                    ("requiredMinutes", MathF.Round(HoursPlaytime % 1 * 60)),
                    ("playtimeHours", Math.Round(departmentPlaytime)),
                    ("playtimeMinutes", Math.Round(departmentPlaytime % 1 * 60))
            );
            return false;
        }

        return true;
    }
}
