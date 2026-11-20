using Content.Shared.Construction.Components;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Construction.Steps;
using Content.Shared.Examine;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Shared.Construction;

/// <summary>
/// Computer boards list the materials the computer frame needs, like machine boards do.
/// </summary>
public sealed partial class ComputerBoardExamineSystem : EntitySystem
{
    private static readonly ProtoId<ConstructionPrototype> ComputerConstruction = "Computer";

    [Dependency] private IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ComputerBoardComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<ComputerBoardComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange
            || !_prototype.TryIndex(ComputerConstruction, out var construction)
            || !_prototype.TryIndex(construction.Graph, out var graph)
            || graph.Path(construction.StartNode, construction.TargetNode) is not { } path)
        {
            return;
        }

        using (args.PushGroup(nameof(ComputerBoardComponent)))
        {
            args.PushMarkup(Loc.GetString("machine-board-component-on-examine-label"));

            var node = graph.Nodes[construction.StartNode];
            foreach (var next in path)
            {
                foreach (var step in node.GetEdge(next.Name)!.Steps)
                {
                    if (step is not MaterialConstructionGraphStep material)
                        continue;

                    var stack = _prototype.Index(material.MaterialPrototypeId);
                    args.PushMarkup(Loc.GetString("machine-board-component-required-element-entry-text",
                        ("amount", material.Amount),
                        ("requiredElement", _prototype.Index(stack.Spawn).Name)));
                }

                node = next;
            }
        }
    }
}
