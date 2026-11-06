using Content.Shared.Examine;
using Robust.Shared.Utility;

// Arcane-Start
using Content.Shared.Construction.Components;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Construction;
using Content.Shared.Construction.Steps;
using Robust.Shared.Prototypes;
using Content.Arcane.Common.Construction.Prototypes;
// Arcane-End

namespace Content.Shared.Construction.Steps
{
    public abstract partial class ArbitraryInsertConstructionGraphStep : EntityInsertConstructionGraphStep
    {
        [DataField] public LocId Name { get; private set; } = string.Empty;

        [DataField] public SpriteSpecifier? Icon { get; private set; }

        public override void DoExamine(ExaminedEvent examinedEvent)
        {
            if (string.IsNullOrEmpty(Name))
                return;

            var stepName = Loc.GetString(Name);
            examinedEvent.PushMarkup(Loc.GetString("construction-insert-arbitrary-entity", ("stepName", stepName)));
        }

        public override ConstructionGuideEntry GenerateGuideEntry()
        {
            var stepName = Loc.GetString(Name);
            return new ConstructionGuideEntry
            {
                Localization = "construction-presenter-arbitrary-step",
                Arguments = new (string, object)[]{("name", stepName)},
                Icon = Icon,
            };
        }
    }

    // Arcane-Start
    [DataDefinition]
    public sealed partial class MachinePartConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {
        [DataField(required: true)]
        public ProtoId<MachinePartPrototype> MachinePart;

        public override bool EntityValid(EntityUid uid, IEntityManager entityManager, IComponentFactory compFactory)
        {
            return entityManager.TryGetComponent(uid, out MachinePartComponent? machinePart) && machinePart.Part == MachinePart;
        }

        public override void DoExamine(ExaminedEvent examinedEvent)
        {
            var localizedPartName = MachinePart.Id;
            if (IoCManager.Resolve<IPrototypeManager>().TryIndex(MachinePart, out var machinePartProto) && !string.IsNullOrWhiteSpace(machinePartProto.Name))
                localizedPartName = Loc.GetString(machinePartProto.Name);

            examinedEvent.PushMarkup(string.IsNullOrEmpty(Name)
                ? Loc.GetString("construction-insert-entity-with-component", ("componentName", localizedPartName))
                : Loc.GetString("construction-insert-exact-entity", ("entityName", Loc.GetString(Name))));
        }

        public override ConstructionGuideEntry GenerateGuideEntry()
        {
            return new ConstructionGuideEntry
            {
                Localization = "construction-presenter-arbitrary-step",
                Arguments = [("name", MachinePart.Id)],
                Icon = Icon,
            };
        }
    }
    // Arcane-End
}
