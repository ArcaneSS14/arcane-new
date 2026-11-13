using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Instruments;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Kitchen;
using Content.Shared.PAI;
using Content.Shared.Popups;
using Content.Shared.Instruments;
using Robust.Shared.Random;
using System.Text;
using Content.Shared.Actions; // Arcane
using Content.Shared.Silicons.StationAi; // Arcane
using Content.Shared.StationAi; // Arcane

namespace Content.Server.PAI;

public sealed partial class PAISystem : EntitySystem
{
    [Dependency] private InstrumentSystem _instrumentSystem = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ToggleableGhostRoleSystem _toggleableGhostRole = default!;
    [Dependency] private SharedActionsSystem _actions = default!; // Arcane
    [Dependency] private SharedStationAiSystem _stationAi = default!; // Arcane

    /// <summary>
    /// Possible symbols that can be part of a scrambled pai's name.
    /// </summary>
    private static readonly char[] SYMBOLS = new[] { '#', '~', '-', '@', '&', '^', '%', '$', '*', ' ' };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PAIComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<PAIComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<PAIComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<PAIComponent, BeingMicrowavedEvent>(OnMicrowaved);
        SubscribeLocalEvent<PAIComponent, ComponentShutdown>(OnShutdown); // Arcane
        SubscribeLocalEvent<PAIComponent, TogglePAICameraEvent>(OnToggleCamera); // Arcane
    }

    private void OnUseInHand(EntityUid uid, PAIComponent component, UseInHandEvent args)
    {
        // Not checking for Handled because ToggleableGhostRoleSystem already marks it as such.

        if (!TryComp<MindContainerComponent>(uid, out var mind) || !mind.HasMind)
        // <Trauma> - set the name now not later when the user might be deleted
        {
            component.LastUser = args.User;
            component.LastName = Loc.GetString("pai-system-pai-name", ("owner", args.User));
        }
        // </Trauma>
    }

    private void OnMindAdded(EntityUid uid, PAIComponent component, MindAddedMessage args)
    {
        // Arcane-Start: optional camera action
        if (component.CameraAction is { } action)
            _actions.AddAction(uid, ref component.CameraActionEntity, action.Id);
        // Arcane-End

        if (component.LastUser == null)
            return;

        // Ownership tag
        var val = component.LastName; // Trauma - use the saved name

        // TODO Identity? People shouldn't dox-themselves by carrying around a PAI.
        // But having the pda's name permanently be "old lady's PAI" is weird.
        // Changing the PAI's identity in a way that ties it to the owner's identity also seems weird.
        // Cause then you could remotely figure out information about the owner's equipped items.

        _metaData.SetEntityName(uid, val);
    }

    private void OnMindRemoved(EntityUid uid, PAIComponent component, MindRemovedMessage args)
    {
        // Mind was removed, shutdown the PAI.
        DisableCamera(uid, component); // Arcane
        PAITurningOff(uid);
    }

    // Arcane-Start: opt-in camera for station AI
    private void OnToggleCamera(Entity<PAIComponent> entity, ref TogglePAICameraEvent args)
    {
        if (args.Handled || args.Performer != entity.Owner || entity.Comp.CameraAction is null ||
            entity.Comp.CameraActionEntity != args.Action.Owner ||
            !TryComp<MindContainerComponent>(entity, out var mind) || !mind.HasMind ||
            !TryComp<StationAiVisionComponent>(entity, out var vision))
            return;

        args.Handled = true;
        _stationAi.SetVisionEnabled((entity.Owner, vision), !vision.Enabled);
        _actions.SetToggled(args.Action.AsNullable(), vision.Enabled);
    }

    private void OnShutdown(Entity<PAIComponent> entity, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(entity))
            DisableCamera(entity.Owner, entity.Comp);
    }

    private void DisableCamera(EntityUid uid, PAIComponent component)
    {
        if (component.CameraActionEntity is not { } action)
            return;

        if (TryComp<StationAiVisionComponent>(uid, out var vision))
            _stationAi.SetVisionEnabled((uid, vision), false);

        _actions.RemoveAction(uid, action);
        component.CameraActionEntity = null;
    }
    // Arcane-End

    private void OnMicrowaved(EntityUid uid, PAIComponent comp, BeingMicrowavedEvent args)
    {
        // name will always be scrambled whether it gets bricked or not, this is the reward
        ScrambleName(uid, comp);

        // randomly brick it
        if (_random.Prob(comp.BrickChance))
        {
            _popup.PopupEntity(Loc.GetString(comp.BrickPopup), uid, PopupType.LargeCaution);
            _toggleableGhostRole.Wipe(uid);
            RemComp<PAIComponent>(uid);
            RemComp<ToggleableGhostRoleComponent>(uid);
        }
        else
        {
            // you are lucky...
            _popup.PopupEntity(Loc.GetString(comp.ScramblePopup), uid, PopupType.Large);
        }
    }

    private void ScrambleName(EntityUid uid, PAIComponent comp)
    {
        // create a new random name
        var len = _random.Next(6, 18);
        var name = new StringBuilder(len);
        for (int i = 0; i < len; i++)
        {
            name.Append(_random.Pick(SYMBOLS));
        }

        // add 's pAI to the scrambled name
        var val = Loc.GetString("pai-system-pai-name-raw", ("name", name.ToString()));
        _metaData.SetEntityName(uid, val);
    }
    public void PAITurningOff(EntityUid uid)
    {
        //  Close the instrument interface if it was open
        //  before closing
        if (HasComp<ActiveInstrumentComponent>(uid))
        {
            _instrumentSystem.ToggleInstrumentUi(uid, uid);
        }

        //  Stop instrument
        if (TryComp<InstrumentComponent>(uid, out var instrument))
            _instrumentSystem.Clean(uid, instrument);

        if (TryComp(uid, out MetaDataComponent? metadata))
        {
            var proto = metadata.EntityPrototype;
            if (proto != null)
                _metaData.SetEntityName(uid, proto.Name);
        }
    }
}
