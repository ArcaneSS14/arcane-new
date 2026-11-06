using Content.Shared.Research.Systems;
// Arcane-Start
using Content.Shared.Research;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
// Arcane-End

using Content.Arcane.Common.Research; // Arcane

namespace Content.Client.Research;

// Arcane-Edit-Start
public sealed partial class ResearchSystem : SharedResearchSystem
{
    public List<ResearchPointAmount> GetTechnologyFinalPointCostsForUi(TechnologyDatabaseComponent database, TechnologyPrototype technology)
    {
        return GetTechnologyFinalPointCosts(database, technology);
    }
}
// Arcane-Edit-End
