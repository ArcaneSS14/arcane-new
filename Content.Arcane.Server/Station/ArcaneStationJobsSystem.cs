using Content.Server.Station.Systems;
using Content.Shared.Station.Components;

namespace Content.Arcane.Server.Station;

/// <summary>
/// Applies a station's additional job defaults before the core job counters are initialized.
/// </summary>
public sealed partial class ArcaneStationJobsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        // The core init handler derives both the total and overflow jobs from the completed setup data.
        SubscribeLocalEvent<StationJobsComponent, ComponentInit>(OnJobsInit, before: [typeof(ServerStationJobsSystem)]);
    }

    private void OnJobsInit(Entity<StationJobsComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<ArcaneStationJobsComponent>(ent, out var defaults)
            || !ent.Comp.SetupAvailableJobs.ContainsKey(defaults.RequiredJob))
            return;

        foreach (var (job, slots) in defaults.AdditionalJobs)
        {
            ent.Comp.SetupAvailableJobs.TryAdd(job, (int[]) slots.Clone());
        }
    }
}
