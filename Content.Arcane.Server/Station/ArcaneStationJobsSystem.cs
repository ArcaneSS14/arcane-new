using Content.Server.Station.Systems;
using Content.Shared.Roles;
using Content.Shared.Station.Components;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.Station;

/// <summary>
/// Gives every crewed station one slot each of the Arcane command jobs, as old Arcane did on every map,
/// without editing each map prototype.
/// </summary>
public sealed partial class ArcaneStationJobsSystem : EntitySystem
{
    private static readonly ProtoId<JobPrototype> Captain = "Captain";

    private static readonly ProtoId<JobPrototype>[] ExtraJobs =
    [
        "Adjutant",
        "IAA",
        "CommandMaid",
    ];

    public override void Initialize()
    {
        base.Initialize();

        // Before the job list is built from the station's setup jobs.
        SubscribeLocalEvent<StationInitializedEvent>(OnStationInitialized, before: [typeof(ServerStationJobsSystem)]);
    }

    private void OnStationInitialized(StationInitializedEvent ev)
    {
        if (!TryComp<StationJobsComponent>(ev.Station, out var jobs)
            || !jobs.SetupAvailableJobs.ContainsKey(Captain))
            return;

        foreach (var job in ExtraJobs)
        {
            if (jobs.SetupAvailableJobs.TryAdd(job, [1, 1]))
                jobs.MidRoundTotalJobs += 1;
        }
    }
}
