using Content.Shared.Construction.Components;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Shared.Construction.Prototypes; // Arcane

namespace Content.Server.Construction.Components
{
    [RegisterComponent]
    public sealed partial class MachineFrameComponent : Component
    {
        public const string PartContainerName = "machine_parts";
        public const string BoardContainerName = "machine_board";

        [ViewVariables]
        public bool HasBoard => BoardContainer?.ContainedEntities.Count != 0;

        [ViewVariables]
        public readonly Dictionary<ProtoId<StackPrototype>, int> MaterialProgress = new();

        // Arcane-Start
        [ViewVariables]
        public readonly Dictionary<ProtoId<MachinePartPrototype>, int> PartProgress = new();
        // Arcane-End

        [ViewVariables]
        public readonly Dictionary<string, int> ComponentProgress = new();

        [ViewVariables]
        public readonly Dictionary<ProtoId<TagPrototype>, int> TagProgress = new();

        [ViewVariables]
        public Dictionary<ProtoId<StackPrototype>, int> MaterialRequirements = new();

        // Arcane-Start
        [ViewVariables]
        public Dictionary<ProtoId<MachinePartPrototype>, int> PartRequirements = new();
        // Arcane-End

        [ViewVariables]
        public Dictionary<string, GenericPartInfo> ComponentRequirements = new();

        [ViewVariables]
        public Dictionary<ProtoId<TagPrototype>, GenericPartInfo> TagRequirements = new();

        [ViewVariables]
        public Container BoardContainer = default!;

        [ViewVariables]
        public Container PartContainer = default!;
    }
}
