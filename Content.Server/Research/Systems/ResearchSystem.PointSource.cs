using Content.Server.Power.EntitySystems;
using Content.Server.Research.Components;
using Content.Shared.Research.Components;
using Content.Shared.Research; // Arcane

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    private void InitializeSource()
    {
//        SubscribeLocalEvent<ResearchPointSourceComponent, ResearchServerGetPointsPerSecondEvent>(OnGetPointsPerSecond); // Arcane-Edit
        SubscribeLocalEvent<ResearchPointSourceComponent, ResearchServerGetPointsPerSecondByTypeEvent>(OnGetPointsPerSecondByType); // Arcane
    }

/* // Arcane-Edit: Use OnGetPointsPerSecondByType
    private void OnGetPointsPerSecond(Entity<ResearchPointSourceComponent> source, ref ResearchServerGetPointsPerSecondEvent args)
    {
        // Arcane-Start
        if (TryComp<ResearchServerControlStatusComponent>(args.Server, out var status) && !status.GenerationEnabled)
            return;
        // Arcane-End

        if (CanProduce(source))
            args.Points += source.Comp.PointsPerSecond;
    }
*/

    private bool CanProduce(Entity<ResearchPointSourceComponent> source) // Arcane-Edit: Was public
    {
        return source.Comp.Active && this.IsPowered(source, EntityManager);
    }

    // Arcane-Start
    private void OnGetPointsPerSecondByType(Entity<ResearchPointSourceComponent> source, ref ResearchServerGetPointsPerSecondByTypeEvent args)
    {
        if (TryComp<ResearchServerControlStatusComponent>(args.Server, out var status) && !status.GenerationEnabled)
            return;

        if (!CanProduce(source))
            return;

        if (source.Comp.RequiredInfrastructure != null &&
            (!TryComp<TechnologyDatabaseComponent>(args.Server, out var db) ||
             !db.UnlockedInfrastructure.Contains(source.Comp.RequiredInfrastructure)))
        {
            return;
        }

        args.Points.Add(new ResearchPointAmount
        {
            Type = source.Comp.PointType,
            Amount = source.Comp.PointsPerSecond,
        });
    }
    // Arcane-End
}
