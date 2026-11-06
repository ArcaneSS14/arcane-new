using Content.Server.Construction.Components;
using Content.Shared.Construction.Components;
using Robust.Shared.Containers;
// Arcane-Start
using Content.Shared.Construction.Prototypes;
using Content.Shared.Stacks;
// Arcane-End

// Arcane-Start
using System.Linq;
using Content.Shared.Construction; // Arcane
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Arcane.Common.Construction.Prototypes;
// Arcane-End

namespace Content.Server.Construction;

public sealed partial class ConstructionSystem
{
    [Dependency] private ExamineSystemShared _examineSystem = default!; // Arcane

    private void InitializeMachines()
    {
        SubscribeLocalEvent<MachineComponent, ComponentInit>(OnMachineInit);
        SubscribeLocalEvent<MachineComponent, ComponentStartup>(OnMachineStartup); // Arcane
        SubscribeLocalEvent<MachineComponent, MapInitEvent>(OnMachineMapInit);
    }

    private void OnMachineInit(EntityUid uid, MachineComponent component, ComponentInit args)
    {
        component.BoardContainer = _container.EnsureContainer<Container>(uid, MachineFrameComponent.BoardContainerName);
        component.PartContainer = _container.EnsureContainer<Container>(uid, MachineFrameComponent.PartContainerName);
    }

    // Arcane-Start
    private void OnMachineStartup(EntityUid uid, MachineComponent component, ComponentStartup args)
    {
        if (component.BoardContainer.ContainedEntities.Count == 0)
            return;

        RefreshParts(uid, component);
    }
    // Arcane-End

    private void OnMachineMapInit(EntityUid uid, MachineComponent component, MapInitEvent args)
    {
        CreateBoardAndStockParts(uid, component);
        RefreshParts(uid, component); // Arcane
    }

    private void CreateBoardAndStockParts(EntityUid uid, MachineComponent component)
    {
        // Entity might not be initialized yet.
        var boardContainer = _container.EnsureContainer<Container>(uid, MachineFrameComponent.BoardContainerName);
        var partContainer = _container.EnsureContainer<Container>(uid, MachineFrameComponent.PartContainerName);

        if (string.IsNullOrEmpty(component.Board))
            return;

        // We're done here, let's suppose all containers are correct just so we don't screw SaveLoadSave.
        if (boardContainer.ContainedEntities.Count > 0)
            return;

        var xform = Transform(uid);
        if (!TrySpawnInContainer(component.Board, uid, MachineFrameComponent.BoardContainerName, out var board))
        {
            throw new Exception($"Couldn't insert board with prototype {component.Board} to machine with prototype {Prototype(uid)?.ID ?? "N/A"}!");
        }

        if (!TryComp<MachineBoardComponent>(board, out var machineBoard))
        {
            throw new Exception($"Entity with prototype {component.Board} doesn't have a {nameof(MachineBoardComponent)}!");
        }

        foreach (var (stackType, amount) in machineBoard.StackRequirements)
        {
            var stack = _stackSystem.SpawnAtPosition(amount, stackType, xform.Coordinates);
            if (!_container.Insert(stack, partContainer))
                throw new Exception($"Couldn't insert machine material of type {stackType} to machine with prototype {Prototype(uid)?.ID ?? "N/A"}");
        }

        // Arcane-Start
        foreach (var (partType, amount) in machineBoard.PartRequirements)
        {
            if (!ProtoMan.TryIndex(partType, out var machinePart))
                throw new Exception($"Unknown machine part requirement {partType} for machine with prototype {Prototype(uid)?.ID ?? "N/A"}");

            for (var i = 0; i < amount; i++)
            {
                if (!TrySpawnInContainer(machinePart.StockPartPrototype, uid, MachineFrameComponent.PartContainerName, out _))
                    throw new Exception($"Couldn't insert machine part requirement {partType} to machine with prototype {Prototype(uid)?.ID ?? "N/A"}");
            }
        }
        // Arcane-End

        foreach (var (compName, info) in machineBoard.ComponentRequirements)
        {
            for (var i = 0; i < info.Amount; i++)
            {
                if(!TrySpawnInContainer(info.DefaultPrototype, uid, MachineFrameComponent.PartContainerName, out _))
                    throw new Exception($"Couldn't insert machine component part with default prototype '{compName}' to machine with prototype {Prototype(uid)?.ID ?? "N/A"}");
            }
        }

        foreach (var (tagName, info) in machineBoard.TagRequirements)
        {
            for (var i = 0; i < info.Amount; i++)
            {
                if(!TrySpawnInContainer(info.DefaultPrototype, uid, MachineFrameComponent.PartContainerName, out _))
                    throw new Exception($"Couldn't insert machine component part with default prototype '{tagName}' to machine with prototype {Prototype(uid)?.ID ?? "N/A"}");
            }
        }
    }

    // Arcane-Start
    private void InitializeMachineUpgrades()
    {
        SubscribeLocalEvent<MachineComponent, GetVerbsEvent<ExamineVerb>>(OnMachineExaminableVerb);
    }

    private void OnMachineExaminableVerb(EntityUid uid, MachineComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var markup = new FormattedMessage();
        RaiseLocalEvent(uid, new UpgradeExamineEvent(ref markup));

        if (markup.IsEmpty)
            return;

        if (!FormattedMessage.TryFromMarkup(markup.ToMarkup().TrimEnd('\n'), out markup))
            markup = FormattedMessage.Empty;

        if (markup.IsEmpty)
            return;

        args.Verbs.Add(new ExamineVerb
        {
            Act = () => _examineSystem.SendExamineTooltip(args.User, uid, markup, getVerbs: false, centerAtCursor: false),
            Text = Loc.GetString("machine-upgrade-examinable-verb-text"),
            Message = Loc.GetString("machine-upgrade-examinable-verb-message"),
            Category = VerbCategory.Examine,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/pickup.svg.192dpi.png"))
        });
    }

    public bool GetMachinePartState(EntityUid uid, out MachinePartState state)
    {
        state = new MachinePartState();

        if (!TryComp(uid, out MachinePartComponent? part))
            return false;

        state.Part = part;
        TryComp(uid, out state.Stack);
        return true;
    }

    private List<MachinePartState> GetAllParts(MachineComponent component)
    {
        var parts = new List<MachinePartState>();
        foreach (var entity in component.PartContainer.ContainedEntities)
        {
            if (GetMachinePartState(entity, out var partState))
                parts.Add(partState);
        }

        return parts;
    }

    private Dictionary<ProtoId<MachinePartPrototype>, float> GetPartRatings(List<MachinePartState> partStates)
    {
        var weightedRatings = new Dictionary<ProtoId<MachinePartPrototype>, float>();
        var quantities = new Dictionary<ProtoId<MachinePartPrototype>, float>();

        foreach (var state in partStates)
        {
            var id = state.Part.Part;
            var count = state.Quantity();

            quantities[id] = quantities.GetValueOrDefault(id) + count;
            weightedRatings[id] = weightedRatings.GetValueOrDefault(id) + state.Part.Tier * count;
        }

        var result = new Dictionary<ProtoId<MachinePartPrototype>, float>();
        foreach (var (id, quantity) in quantities)
        {
            result[id] = quantity > 0 ? weightedRatings[id] / quantity : 1f;
        }

        return result;
    }

    public void RefreshParts(EntityUid uid, MachineComponent component)
    {
        var parts = GetAllParts(component);
        var partRatings = GetPartRatings(parts);

        RaiseLocalEvent(uid,
            new RefreshPartsEvent
        {
            Parts = parts,
            PartRatings = partRatings,
        },
        broadcast: true);
    }
    // Arcane-End
}
