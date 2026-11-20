using Content.Server.Construction.Components;
using Content.Shared.Construction;
using Content.Shared.Construction.Components;
using Content.Shared.Examine;

namespace Content.Server.Construction;

public sealed partial class ConstructionSystem
{
    /// <summary>
    /// A node can branch into several finished products (augment boards, for example): list the ones the target path
    /// skips, so players see every option.
    /// </summary>
    private void ExamineAlternativeFinishes(EntityUid uid, ConstructionComponent component, ConstructionGraphEdge targetEdge, ExaminedEvent args)
    {
        if (GetCurrentGraph(uid, component) is not { } graph || GetCurrentNode(uid, component) is not { } node)
            return;

        foreach (var edge in node.Edges)
        {
            if (edge == targetEdge
                || edge.Steps.Count == 0
                || GetNodeFromGraph(graph, edge.Target) is not { Edges.Count: 0 } finish
                || finish.Name == component.DeconstructionNode)
            {
                continue;
            }

            args.PushMarkup(Loc.GetString("construction-component-alternative-step-header"));
            edge.Steps[0].DoExamine(args);
        }
    }
}
