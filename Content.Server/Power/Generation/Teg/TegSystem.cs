using Content.Server.Atmos.EntitySystems;
using Content.Server.Audio;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Examine;
using Content.Shared.NodeContainer;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Power.Generation.Teg;
using Content.Shared.Rounding;
using Robust.Server.GameObjects;
// Arcane-Start
using Content.Shared.Destructible;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Repairable;
using Content.Shared.Tools.Systems;
using Content.Shared.Wires;
// Arcane-End

namespace Content.Server.Power.Generation.Teg;

/// <summary>
/// Handles processing logic for the thermo-electric generator (TEG).
/// </summary>
/// <remarks>
/// <para>
/// The TEG generates power by exchanging heat between gases flowing through its two sides.
/// The gas flows through a "circulator" entity on each side, which have both an inlet and an outlet port.
/// </para>
/// <remarks>
/// Connecting the TEG core to its circulators is implemented via a node group. See <see cref="TegNodeGroup"/>.
/// </remarks>
/// <para>
/// The TEG center does HV power output, and must also be connected to an LV wire for the TEG to function.
/// </para>
/// <para>
/// Unlike in SS13, the TEG actually adjusts gas heat exchange to match the energy demand of the power network.
/// To achieve this, the TEG implements its own ramping logic instead of using the built-in Pow3r ramping.
/// The TEG actually has a maximum output of +n% more than was really generated,
/// which allows Pow3r to draw more power to "signal" that there is more network load.
/// The ramping is also exponential instead of linear like in normal Pow3r.
/// This system does mean a fully-loaded TEG creates +n% power out of thin air, but this is considered acceptable.
/// </para>
/// </remarks>
/// <seealso cref="TegGeneratorComponent"/>
/// <seealso cref="TegCirculatorComponent"/>
/// <seealso cref="TegNodeGroup"/>
/// <seealso cref="TegSensorPayload"/>
public sealed partial class TegSystem : EntitySystem
{
    /// <summary>
    /// Node name for the TEG part connection nodes (<see cref="TegNodeGroup"/>).
    /// </summary>
    private const string NodeNameTeg = "teg";

    /// <summary>
    /// Node name for the inlet pipe of a circulator.
    /// </summary>
    private const string NodeNameInlet = "inlet";

    /// <summary>
    /// Node name for the outlet pipe of a circulator.
    /// </summary>
    private const string NodeNameOutlet = "outlet";

    [Dependency] private AmbientSoundSystem _ambientSound = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private PointLightSystem _pointLight = default!;
    [Dependency] private SharedPowerReceiverSystem _receiver = default!;
    [Dependency] private EntityQuery<NodeContainerComponent> _nodeContainerQuery = default!;

    // Arcane-Start
    [Dependency] private SharedToolSystem _tool = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    private const string PulsingQuality = "Pulsing";
    private const string PryingQuality = "Prying";
    private static readonly Color HotLightColor = Color.FromHex("#FF3232");
    private static readonly Color ColdLightColor = Color.FromHex("#4F82FF");
    // Arcane-End

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TegGeneratorComponent, AtmosDeviceUpdateEvent>(GeneratorUpdate);
        SubscribeLocalEvent<TegGeneratorComponent, PowerChangedEvent>(GeneratorPowerChange);

        SubscribeLocalEvent<TegGeneratorComponent, ExaminedEvent>(GeneratorExamined);
        // Arcane-Start
        SubscribeLocalEvent<TegGeneratorComponent, ComponentStartup>(OnGeneratorStartup);
        SubscribeLocalEvent<TegGeneratorComponent, ComponentShutdown>(OnGeneratorShutdown);
        SubscribeLocalEvent<TegGeneratorComponent, AnchorStateChangedEvent>(OnGeneratorAnchorChanged);
        SubscribeLocalEvent<TegGeneratorComponent, BreakageEventArgs>(OnGeneratorBreakage);
        SubscribeLocalEvent<TegGeneratorComponent, RepairedEvent>(OnGeneratorRepaired);
        SubscribeLocalEvent<TegGeneratorComponent, PanelChangedEvent>(OnGeneratorPanelChanged);
        SubscribeLocalEvent<TegCirculatorComponent, InteractUsingEvent>(OnCirculatorInteract);
        SubscribeLocalEvent<TegCirculatorComponent, ExaminedEvent>(OnCirculatorExamined);
        SubscribeLocalEvent<TegCirculatorComponent, ComponentStartup>(OnCirculatorStartup);
        SubscribeLocalEvent<TegCirculatorComponent, ComponentShutdown>(OnCirculatorShutdown);
        SubscribeLocalEvent<TegCirculatorComponent, AnchorStateChangedEvent>(OnCirculatorAnchorChanged);
        SubscribeLocalEvent<TegCirculatorComponent, BreakageEventArgs>(OnCirculatorBreakage);
        SubscribeLocalEvent<TegCirculatorComponent, RepairedEvent>(OnCirculatorRepaired);
        SubscribeLocalEvent<TegCirculatorComponent, PanelChangedEvent>(OnCirculatorPanelChanged);
        SubscribeLocalEvent<TegCirculatorComponent, TegCirculatorDoAfterEvent>(OnCirculatorDoAfter);
        // Arcane-End
    }

    private void GeneratorExamined(EntityUid uid, TegGeneratorComponent component, ExaminedEvent args)
    {
        if (!component.Connected) // Arcane-Edit: Check whether the generator is connected.
        {
            args.PushMarkup(Loc.GetString("teg-generator-examine-links")); // Arcane-Edit: Explain automatic assembly requirements.
        }
        else
        {
            var supplier = Comp<PowerSupplierComponent>(uid);

            using (args.PushGroup(nameof(TegGeneratorComponent)))
            {
                args.PushMarkup(Loc.GetString("teg-generator-examine-power", ("power", supplier.CurrentSupply)));
                args.PushMarkup(Loc.GetString("teg-generator-examine-power-max-output", ("power", supplier.MaxSupply)));
                // Arcane-Start
                if (args.IsInDetailsRange && GetNodeGroup(uid) is { IsFullyBuilt: true } group)
                {
                    PushCirculatorReadings(group.CirculatorA!.Owner, args);
                    PushCirculatorReadings(group.CirculatorB!.Owner, args);
                }
                // Arcane-End
            }
        }
    }

    private void GeneratorUpdate(EntityUid uid, TegGeneratorComponent component, ref AtmosDeviceUpdateEvent args)
    {
        var supplier = Comp<PowerSupplierComponent>(uid);
        var powerReceiver = Comp<ApcPowerReceiverComponent>(uid);
        if (!powerReceiver.Powered)
        {
            supplier.MaxSupply = 0;
            return;
        }

        var tegGroup = GetNodeGroup(uid);
        // Arcane-Edit-Start: Require an intact connected assembly.
        if (!component.Connected || component.Broken || tegGroup is not { IsFullyBuilt: true })
        {
            supplier.MaxSupply = 0;
            return;
        }
        // Arcane-Edit-End

        var circA = tegGroup.CirculatorA!.Owner;
        var circB = tegGroup.CirculatorB!.Owner;

        var (inletA, outletA) = GetPipes(circA);
        var (inletB, outletB) = GetPipes(circB);

        var (airA, δpA) = GetCirculatorAirTransfer(inletA.Air, outletA.Air);
        var (airB, δpB) = GetCirculatorAirTransfer(inletB.Air, outletB.Air);

        var cA = _atmosphere.GetHeatCapacity(airA, true);
        var cB = _atmosphere.GetHeatCapacity(airB, true);

        // Shift ramp position based on demand and generation from previous tick.
        var curRamp = component.RampPosition;
        var lastDraw = supplier.CurrentSupply;
        curRamp = MathHelper.Clamp(lastDraw, curRamp / component.RampFactor, curRamp * component.RampFactor);
        curRamp = MathF.Max(curRamp, component.RampMinimum);
        component.RampPosition = curRamp;

        var electricalEnergy = 0f;

        // Arcane-Start
        var modeHotA = Comp<TegCirculatorComponent>(circA).Mode == TegCirculatorMode.Hot;
        var correctTemperatures = modeHotA ? airA.Temperature > airB.Temperature : airB.Temperature > airA.Temperature;
        // Arcane-End
        if (airA.Pressure > 0 && airB.Pressure > 0 && correctTemperatures && // Arcane-Edit: Require the hot loop to be hotter than the cold loop.
            Comp<TegCirculatorComponent>(circA).Mode != Comp<TegCirculatorComponent>(circB).Mode) // Arcane
        {
            var hotA = airA.Temperature > airB.Temperature;

            // Calculate thermal and electrical energy transfer between the two sides.
            // Assume temperature equalizes, i.e. Ta*cA + Tb*cB = Tf*(cA+cB)
            var Tf = (airA.Temperature * cA + airB.Temperature * cB) / (cA + cB);
            // The maximum energy we can extract is (Ta - Tf)*cA, which is equal to (Tf - Tb)*cB
            var Wmax = MathF.Abs(airA.Temperature - Tf) * cA;

            var N = component.ThermalEfficiency;

            // Calculate Carnot efficiency
            var Thot = hotA ? airA.Temperature : airB.Temperature;
            var Tcold = hotA ? airB.Temperature : airA.Temperature;
            var Nmax = 1 - Tcold / Thot;
            N = MathF.Min(N, Nmax); // clamp by Carnot efficiency

            // Reduce efficiency at low temperature differences to encourage burn chambers (instead
            // of just feeding the TEG room temperature gas from an infinite gas miner).
            var dT = Thot - Tcold;
            N *= MathF.Tanh(dT/700); // https://www.wolframalpha.com/input?i=tanh(x/700)+from+0+to+1000

            var transfer = Wmax * N;
            electricalEnergy = transfer * component.PowerFactor;
            var outTransfer = transfer * (1 - component.ThermalEfficiency);

            // Adjust thermal energy in transferred gas mixtures.
            if (hotA)
            {
                // A -> B
                airA.Temperature -= transfer / cA;
                airB.Temperature += outTransfer / cB;
            }
            else
            {
                // B -> A
                airA.Temperature += outTransfer / cA;
                airB.Temperature -= transfer / cB;
            }
        }

        component.LastGeneration = electricalEnergy;

        // Turn energy (at atmos tick rate) into wattage.
        var power = electricalEnergy / args.dt;
        // Add ramp factor. This magics slight power into existence, but allows us to ramp up.
        supplier.MaxSupply = power * component.RampFactor;

        var circAComp = Comp<TegCirculatorComponent>(circA);
        var circBComp = Comp<TegCirculatorComponent>(circB);

        circAComp.LastPressureDelta = δpA;
        circAComp.LastMolesTransferred = airA.TotalMoles;
        circBComp.LastPressureDelta = δpB;
        circBComp.LastMolesTransferred = airB.TotalMoles;

        _atmosphere.Merge(outletA.Air, airA);
        _atmosphere.Merge(outletB.Air, airB);

        UpdateAppearance(uid, component, powerReceiver, tegGroup);
    }

    private void UpdateAppearance(
        EntityUid uid,
        TegGeneratorComponent component,
        ApcPowerReceiverComponent powerReceiver,
        TegNodeGroup nodeGroup)
    {
        // Arcane-Start
        var connected = component.Connected && nodeGroup.IsFullyBuilt && !component.Broken;
        _appearance.SetData(uid, TegVisuals.MachineState, component.Broken
            ? TegMachineState.Broken : connected ? TegMachineState.Assembled : TegMachineState.Unassembled);
        // Arcane-End
        int powerLevel;
        if (powerReceiver.Powered && connected && component.LastGeneration > 0) // Arcane-Edit: Show output only while the generator is operating.
        {
            // Arcane-Edit-Start: Use generated power for the output animation.
            powerLevel = ContentHelpers.RoundToLevels(
                Comp<PowerSupplierComponent>(uid).MaxSupply / component.RampFactor,
                component.MaxVisualPower,
                12);
            // Arcane-Edit-End
        }
        else
        {
            powerLevel = 0;
        }

        _ambientSound.SetAmbience(uid, powerLevel >= 1);
        // TODO: Ok so this introduces popping which is a major shame big rip.
        // _ambientSound.SetVolume(uid, MathHelper.Lerp(component.VolumeMin, component.VolumeMax, MathHelper.Clamp01(component.RampPosition / component.MaxVisualPower)));

        _appearance.SetData(uid, TegVisuals.PowerOutput, powerLevel);

        if (nodeGroup.IsFullyBuilt)
        {
            // Arcane-Edit-Start: Refresh circulator visuals from assembly readiness.
            UpdateCirculatorAppearance(nodeGroup.CirculatorA!.Owner, powerReceiver.Powered && connected);
            UpdateCirculatorAppearance(nodeGroup.CirculatorB!.Owner, powerReceiver.Powered && connected);
            // Arcane-Edit-End
        }
    }

    [Access(typeof(TegNodeGroup))]
    public void UpdateGeneratorConnectivity(
        EntityUid uid,
        TegNodeGroup group,
        TegGeneratorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var powerReceiver = Comp<ApcPowerReceiverComponent>(uid);

        // Arcane-Start
        if (group.IsFullyBuilt)
        {
            Comp<TegCirculatorComponent>(group.CirculatorA!.Owner).Mode ??= TegCirculatorMode.Hot;
            Comp<TegCirculatorComponent>(group.CirculatorB!.Owner).Mode ??= TegCirculatorMode.Cold;
        }

        component.Connected = IsAssemblyReady(uid, component, group);

        if (!component.Connected)
        {
            component.LastGeneration = 0;
            component.RampPosition = component.RampMinimum;
            Comp<PowerSupplierComponent>(uid).MaxSupply = 0;
        }
        // Arcane-End
        _receiver.SetPowerDisabled(uid, !component.Connected, powerReceiver); // Arcane-Edit: Disable power when the assembly is disconnected.
        UpdateAppearance(uid, component, powerReceiver, group);
    }

    [Access(typeof(TegNodeGroup))]
    public void UpdateCirculatorConnectivity(
        EntityUid uid,
        TegNodeGroup group,
        TegCirculatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        // Arcane-Edit-Start: Read connection and power state from the generator.
        var powered = group.Generator != null &&
            TryComp<TegGeneratorComponent>(group.Generator.Owner, out var generator) && generator.Connected &&
            Comp<ApcPowerReceiverComponent>(group.Generator.Owner).Powered;
        UpdateCirculatorAppearance((uid, component), powered);
        // Arcane-Edit-End
    }

    private void UpdateCirculatorAppearance(Entity<TegCirculatorComponent?> ent, bool powered)
    {
        if (!Resolve(ent, ref ent.Comp) || TerminatingOrDeleted(ent)) // Arcane-Edit: Skip parts queued for deletion.
            return;

        var circ = ent.Comp;

        TegCirculatorSpeed speed;
        if (powered && circ.LastPressureDelta > 0 && circ.LastMolesTransferred > 0)
        {
            if (circ.LastPressureDelta > circ.VisualSpeedDelta)
                speed = TegCirculatorSpeed.SpeedFast;
            else
                speed = TegCirculatorSpeed.SpeedSlow;
        }
        else
        {
            speed = TegCirculatorSpeed.SpeedStill;
        }

        // Arcane-Edit-Start: Use persistent loop modes for colors and animation.
        var connected = IsConnected(ent);
        if (!powered)
        {
            circ.LastPressureDelta = 0;
            circ.LastMolesTransferred = 0;
        }

        var hot = circ.Mode != TegCirculatorMode.Cold;
        var state = !powered || circ.Broken ? TegCirculatorState.Off : speed switch
        {
            TegCirculatorSpeed.SpeedStill => TegCirculatorState.Idle,
            TegCirculatorSpeed.SpeedFast => hot ? TegCirculatorState.HotFast : TegCirculatorState.ColdFast,
            _ => hot ? TegCirculatorState.HotSlow : TegCirculatorState.ColdSlow,
        };
        _appearance.SetData(ent, TegVisuals.Flipped, circ.Flipped);
        _appearance.SetData(ent, TegVisuals.MachineState, circ.Broken ? TegMachineState.Broken
            : connected ? circ.Flipped ? TegMachineState.AssembledFlipped : TegMachineState.Assembled
            : circ.Flipped ? TegMachineState.UnassembledFlipped : TegMachineState.Unassembled);
        _appearance.SetData(ent, TegVisuals.CirculatorState, state);

        if (!_pointLight.TryGetLight(ent, out var light))
            return;

        _pointLight.SetEnabled(ent, powered && !circ.Broken, light);
        _pointLight.SetColor(ent, speed == TegCirculatorSpeed.SpeedStill
            ? Color.White : hot ? HotLightColor : ColdLightColor, light);
        _pointLight.SetRadius(ent, speed switch
        {
            TegCirculatorSpeed.SpeedFast => 3f,
            TegCirculatorSpeed.SpeedSlow => 2f,
            _ => 1f,
        }, light);
        _pointLight.SetEnergy(ent, speed == TegCirculatorSpeed.SpeedFast ? 2f : 1f, light);
        // Arcane-Edit-End
    }

    private void GeneratorPowerChange(EntityUid uid, TegGeneratorComponent component, ref PowerChangedEvent args)
    {
        // TODO: I wish power events didn't go out on shutdown.
        if (TerminatingOrDeleted(uid))
            return;

        var nodeGroup = GetNodeGroup(uid);
        if (nodeGroup == null)
            return;

        // Arcane-Start
        if (!args.Powered)
        {
            component.LastGeneration = 0;
            Comp<PowerSupplierComponent>(uid).MaxSupply = 0;
        }
        // Arcane-End
        UpdateAppearance(uid, component, Comp<ApcPowerReceiverComponent>(uid), nodeGroup);
    }

    /// <returns>Null if the node group is not yet available. This can happen during initialization.</returns>
    private TegNodeGroup? GetNodeGroup(EntityUid uidGenerator)
    {
        // Arcane-Edit-Start: Node containers may be absent during component startup or shutdown.
        if (!_nodeContainerQuery.TryComp(uidGenerator, out var nodeContainer))
            return null;
        // Arcane-Edit-End

        // Arcane-Edit-Start: Find the TEG group from either machine part.
        if (!nodeContainer.Nodes.TryGetValue(NodeNameTeg, out var tegNode) &&
            !nodeContainer.Nodes.TryGetValue("circulator", out tegNode))
        // Arcane-Edit-End
            return null;

        if (tegNode.NodeGroup is not TegNodeGroup tegGroup)
            return null;

        return tegGroup;
    }

    private static (GasMixture, float δp) GetCirculatorAirTransfer(GasMixture airInlet, GasMixture airOutlet)
    {
        var n1 = airInlet.TotalMoles;
        var n2 = airOutlet.TotalMoles;
        var p1 = airInlet.Pressure;
        var p2 = airOutlet.Pressure;
        var V1 = airInlet.Volume;
        var V2 = airOutlet.Volume;
        var T1 = airInlet.Temperature;
        var T2 = airOutlet.Temperature;

        var δp = p1 - p2;

        var denom = T1 * V2 + T2 * V1;

        if (δp > 0 && p1 > 0 && denom > 0)
        {
            var transferMoles = n1 - (n1 + n2) * T2 * V1 / denom;
            return (airInlet.Remove(transferMoles), δp);
        }

        return (new GasMixture(), δp);
    }

    private (PipeNode inlet, PipeNode outlet) GetPipes(EntityUid uidCirculator)
    {
        var nodeContainer = _nodeContainerQuery.GetComponent(uidCirculator);
        var inlet = (PipeNode) nodeContainer.Nodes[NodeNameInlet];
        var outlet = (PipeNode) nodeContainer.Nodes[NodeNameOutlet];

        return Comp<TegCirculatorComponent>(uidCirculator).Flipped ? (outlet, inlet) : (inlet, outlet); // Arcane-Edit: Swap logical inlet and outlet when flipped.
    }

    [SubscribeLocalEvent]
    private void OnSyncPayload(Entity<TegGeneratorComponent> ent, ref DeviceNetworkPacketEvent<TegSensorSyncPayload> args)
    {
        var group = GetNodeGroup(ent.Owner);
        if (group is not { IsFullyBuilt: true })
            return;

        var supplier = Comp<PowerSupplierComponent>(ent);

        var dataPayload = new TegSensorPayload
        {
            CirculatorA = GetCirculatorSensorData(group.CirculatorA!.Owner),
            CirculatorB = GetCirculatorSensorData(group.CirculatorB!.Owner),
            LastGeneration = ent.Comp.LastGeneration,
            PowerOutput = supplier.CurrentSupply,
            RampPosition = ent.Comp.RampPosition,
        };

        _deviceNetwork.SendPacket(ent.Owner, args.SenderAddress, ref dataPayload);
    }

    private Circulator GetCirculatorSensorData(EntityUid circulator)
    {
        var (inlet, outlet) = GetPipes(circulator);

        return new Circulator(
            inlet.Air.Pressure,
            outlet.Air.Pressure,
            inlet.Air.Temperature,
            outlet.Air.Temperature);
    }

    // Arcane-Start: Reuse the existing tool and machine lifecycle.
    private bool IsAssemblyReady(EntityUid uid, TegGeneratorComponent component, TegNodeGroup group)
    {
        if (TerminatingOrDeleted(uid) || !Transform(uid).Anchored || component.Broken || IsPanelOpen(uid) || !group.IsFullyBuilt)
            return false;

        return IsCirculatorReady(group.CirculatorA!.Owner) && IsCirculatorReady(group.CirculatorB!.Owner);
    }

    private bool IsCirculatorReady(EntityUid uid) => !TerminatingOrDeleted(uid) &&
        !Comp<TegCirculatorComponent>(uid).Broken && Transform(uid).Anchored && !IsPanelOpen(uid);

    private bool IsPanelOpen(EntityUid uid) => TryComp<WiresPanelComponent>(uid, out var panel) && panel.Open;

    private bool IsConnected(EntityUid uid) => GetNodeGroup(uid) is { Generator: { } generator } &&
        TryComp<TegGeneratorComponent>(generator.Owner, out var component) && component.Connected;

    private void ResetCirculator(TegNodeCirculator? node)
    {
        if (node != null && !TerminatingOrDeleted(node.Owner))
            UpdateCirculatorAppearance(node.Owner, false);
    }

    private void RefreshGenerator(EntityUid uid)
    {
        if (GetNodeGroup(uid) is { Generator: { } generator } group &&
            TryComp<TegGeneratorComponent>(generator.Owner, out var component))
        {
            ResetCirculator(group.CirculatorA);
            ResetCirculator(group.CirculatorB);
            UpdateGeneratorConnectivity(generator.Owner, group, component);
        }
    }

    private void OnCirculatorInteract(Entity<TegCirculatorComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || IsPanelOpen(ent))
            return;

        var mode = _tool.HasQuality(args.Used, PulsingQuality);
        var flip = _tool.HasQuality(args.Used, PryingQuality);
        if (!mode && !flip)
            return;

        args.Handled = true;
        _tool.UseTool(args.Used, args.User, ent, mode ? 2f : 5f, mode ? PulsingQuality : PryingQuality,
            new TegCirculatorDoAfterEvent { ChangeMode = mode, Mode = ent.Comp.Mode, Flipped = ent.Comp.Flipped });
    }

    private void OnCirculatorDoAfter(Entity<TegCirculatorComponent> ent, ref TegCirculatorDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Used is not { } tool ||
            !_tool.HasQuality(tool, args.ChangeMode ? PulsingQuality : PryingQuality) ||
            args.Mode != ent.Comp.Mode || args.Flipped != ent.Comp.Flipped ||
            IsPanelOpen(ent))
            return;

        args.Handled = true;
        var mode = args.ChangeMode;
        if (mode)
            ent.Comp.Mode = ent.Comp.Mode == TegCirculatorMode.Cold ? TegCirculatorMode.Hot : TegCirculatorMode.Cold;
        else
            ent.Comp.Flipped = !ent.Comp.Flipped;
        var group = GetNodeGroup(ent);
        if (group?.Generator is { } generator && TryComp<TegGeneratorComponent>(generator.Owner, out var center))
        {
            center.LastGeneration = 0;
            Comp<PowerSupplierComponent>(generator.Owner).MaxSupply = 0;
            ResetCirculator(group.CirculatorA);
            ResetCirculator(group.CirculatorB);
            UpdateAppearance(generator.Owner, center, Comp<ApcPowerReceiverComponent>(generator.Owner), group);
        }
        else
            UpdateCirculatorAppearance(ent.AsNullable(), false);
        _popup.PopupEntity(Loc.GetString(mode ? "teg-mode-changed" : "teg-flipped",
            ("mode", Loc.GetString(ent.Comp.Mode == TegCirculatorMode.Cold ? "teg-mode-cold" : "teg-mode-hot"))), ent, args.User);
    }

    private void OnCirculatorExamined(Entity<TegCirculatorComponent> ent, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
            PushCirculatorReadings(ent, args);
    }

    private void PushCirculatorReadings(EntityUid uid, ExaminedEvent args)
    {
        var component = Comp<TegCirculatorComponent>(uid);
        var readings = GetCirculatorSensorData(uid);
        var mode = Loc.GetString(component.Mode == TegCirculatorMode.Cold ? "teg-mode-cold" : "teg-mode-hot");
        args.PushMarkup(Loc.GetString("teg-circulator-readings", ("mode", mode),
            ("inPressure", readings.InletPressure.ToString("F1")), ("outPressure", readings.OutletPressure.ToString("F1")),
            ("inTemperature", readings.InletTemperature.ToString("F1")), ("outTemperature", readings.OutletTemperature.ToString("F1"))));
        args.PushMarkup(Loc.GetString(component.Broken ? "teg-state-broken" : IsConnected(uid) ? "teg-state-connected" : "teg-state-disconnected"));
    }

    private void OnGeneratorStartup(Entity<TegGeneratorComponent> ent, ref ComponentStartup args)
    {
        _appearance.SetData(ent, TegVisuals.MachineState, ent.Comp.Broken ? TegMachineState.Broken : TegMachineState.Unassembled);
    }

    private void OnCirculatorStartup(Entity<TegCirculatorComponent> ent, ref ComponentStartup args) => UpdateCirculatorAppearance(ent.AsNullable(), false);
    private void OnGeneratorShutdown(Entity<TegGeneratorComponent> ent, ref ComponentShutdown args) => RefreshGenerator(ent);
    private void OnCirculatorShutdown(Entity<TegCirculatorComponent> ent, ref ComponentShutdown args) => RefreshGenerator(ent);

    private void OnGeneratorAnchorChanged(Entity<TegGeneratorComponent> ent, ref AnchorStateChangedEvent args) => RefreshGenerator(ent);

    private void OnCirculatorAnchorChanged(Entity<TegCirculatorComponent> ent, ref AnchorStateChangedEvent args)
    {
        RefreshGenerator(ent);
        if (!args.Anchored)
            UpdateCirculatorAppearance(ent.AsNullable(), false);
    }

    private void OnGeneratorBreakage(Entity<TegGeneratorComponent> ent, ref BreakageEventArgs args)
    {
        ent.Comp.Broken = true;
        RefreshGenerator(ent);
        _appearance.SetData(ent, TegVisuals.MachineState, TegMachineState.Broken);
    }

    private void OnCirculatorBreakage(Entity<TegCirculatorComponent> ent, ref BreakageEventArgs args)
    {
        ent.Comp.Broken = true;
        RefreshGenerator(ent);
        UpdateCirculatorAppearance(ent.AsNullable(), false);
    }

    private void OnGeneratorRepaired(Entity<TegGeneratorComponent> ent, ref RepairedEvent args)
    {
        if (!ent.Comp.Broken)
            return;
        ent.Comp.Broken = false;
        RefreshGenerator(ent);
    }

    private void OnCirculatorRepaired(Entity<TegCirculatorComponent> ent, ref RepairedEvent args)
    {
        if (!ent.Comp.Broken)
            return;
        ent.Comp.Broken = false;
        RefreshGenerator(ent);
        if (!IsConnected(ent))
            UpdateCirculatorAppearance(ent.AsNullable(), false);
    }

    private void OnGeneratorPanelChanged(Entity<TegGeneratorComponent> ent, ref PanelChangedEvent args)
    {
        RefreshGenerator(ent);
    }

    private void OnCirculatorPanelChanged(Entity<TegCirculatorComponent> ent, ref PanelChangedEvent args)
    {
        RefreshGenerator(ent);
    }
    // Arcane-End
}
