using System;
using System.Linq;
using Content.Server.Station.Systems;
using Content.Shared.Station.Components;

namespace Content.Arcane.Server.Station;

/// <summary>
/// Applies a station's additional job defaults before the core job list is initialized.
/// </summary>
public sealed partial class ArcaneStationJobsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationInitializedEvent>(OnStationInitialized, before: [typeof(ServerStationJobsSystem)]);
    }

    private void OnStationInitialized(StationInitializedEvent args)
    {
        if (!TryComp<StationJobsComponent>(args.Station, out var jobs)
            || !TryComp<ArcaneStationJobsComponent>(args.Station, out var defaults)
            || !jobs.SetupAvailableJobs.ContainsKey(defaults.RequiredJob))
            return;

        foreach (var (job, slots) in defaults.AdditionalJobs)
        {
            if (!jobs.SetupAvailableJobs.TryAdd(job, (int[]) slots.Clone()))
                continue;

            // Component init has already cached these counters before the station startup event.
            jobs.MidRoundTotalJobs += Math.Max(slots[1], 0);
            if (slots[0] < 0)
                jobs.OverflowJobs = jobs.OverflowJobs.Append(job).ToHashSet();
        }
    }
}
