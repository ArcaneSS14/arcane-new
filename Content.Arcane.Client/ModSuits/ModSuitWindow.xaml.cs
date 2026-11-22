using System.Linq;
using Content.Arcane.Shared.ModSuits;
using Content.Arcane.Shared.ModSuits.Components;
using Content.Arcane.Shared.ModSuits.Systems;
using Content.Client.UserInterface.Controls;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Wires;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Arcane.Client.ModSuits;

[GenerateTypedNameReferences]
public sealed partial class ModSuitWindow : FancyWindow
{
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IPlayerManager _players = default!;

    private readonly ModSuitSystem _suits;
    private readonly SharedBatterySystem _battery;
    private readonly SharedWiresSystem _wires;
    private readonly Dictionary<string, Button> _parts = [];
    private readonly Dictionary<EntityUid, ModuleRow> _rows = [];
    private EntityUid _suit;
    private float _nextRefresh;

    public event Action? OnActivate;
    public event Action? OnToggleAll;
    public event Action<string>? OnTogglePart;
    public event Action<NetEntity>? OnUseModule;
    public event Action<NetEntity>? OnPinModule;
    public event Action<NetEntity>? OnRemoveModule;

    public ModSuitWindow()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        _suits = _entities.System<ModSuitSystem>();
        _battery = _entities.System<SharedBatterySystem>();
        _wires = _entities.System<SharedWiresSystem>();
        ActivateButton.OnPressed += _ => OnActivate?.Invoke();
        ToggleAllButton.OnPressed += _ => OnToggleAll?.Invoke();
    }

    public void SetSuit(EntityUid suit)
    {
        _suit = suit;
        Refresh();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        _nextRefresh -= args.DeltaSeconds;
        if (_nextRefresh > 0f)
            return;

        _nextRefresh = 0.25f;
        Refresh();
    }

    private void Refresh()
    {
        if (!_entities.TryGetComponent(_suit, out ModSuitComponent? component))
            return;

        var suit = (_suit, component);
        var theme = _prototypes.Index(component.Theme);
        var state = component.State;
        var stateName = Loc.GetString($"modsuit-ui-state-{state.ToString().ToLowerInvariant()}");
        StatusLabel.Text = Loc.GetString("modsuit-ui-status", ("state", stateName));
        ComplexityLabel.Text = Loc.GetString("modsuit-ui-complexity", ("current", _suits.GetComplexity(suit)), ("max", theme.ComplexityMax));
        CoreLabel.Text = _suits.GetCore(suit) is { } core && _entities.TryGetComponent(core, out MetaDataComponent? metadata)
            ? Loc.GetString("modsuit-ui-core", ("core", metadata.EntityName))
            : Loc.GetString("modsuit-ui-core-none");
        ChargeLabel.Text = _suits.GetBattery(suit) is { } battery
            ? Loc.GetString("modsuit-ui-charge", ("percent", MathF.Round(_battery.GetChargeLevel(battery.AsNullable()) * 100f)),
                ("drain", _suits.GetTotalDrain(suit).ToString("0.##")))
            : Loc.GetString("modsuit-ui-no-core");

        var canDeploy = component.Wearer != null && !component.Deploying && component.State == ModSuitState.Inactive;
        ActivateButton.Text = Loc.GetString(component.State == ModSuitState.Active ? "modsuit-ui-deactivate" : "modsuit-ui-activate");
        ActivateButton.Disabled = component.Wearer == null || component.Deploying
            || component.State is ModSuitState.Activating or ModSuitState.Deactivating
            || component.State == ModSuitState.Inactive && !component.Powered;
        ToggleAllButton.Disabled = !canDeploy;
        RefreshParts(suit, canDeploy);
        RefreshModules(suit);
    }

    private void RefreshParts(Entity<ModSuitComponent> suit, bool canDeploy)
    {
        if (!_parts.Keys.SequenceEqual(suit.Comp.PartEntities.Keys))
        {
            _parts.Clear();
            PartsGrid.RemoveAllChildren();
            foreach (var slot in suit.Comp.PartEntities.Keys)
            {
                var button = new Button { Text = Loc.GetString($"modsuit-ui-part-{slot.ToLowerInvariant()}"), ToggleMode = true };
                button.OnPressed += _ => OnTogglePart?.Invoke(slot);
                PartsGrid.AddChild(button);
                _parts.Add(slot, button);
            }
        }

        foreach (var (slot, button) in _parts)
        {
            button.Pressed = _suits.IsPartDeployed(suit, slot);
            button.Disabled = !canDeploy;
        }
    }

    private void RefreshModules(Entity<ModSuitComponent> suit)
    {
        if (suit.Comp.ModulesContainer is not { } container)
            return;

        if (!_rows.Keys.SequenceEqual(container.ContainedEntities))
        {
            _rows.Clear();
            ModulesList.RemoveAllChildren();
            foreach (var module in container.ContainedEntities)
            {
                if (_entities.TryGetComponent(module, out ModModuleComponent? component)
                    && _entities.TryGetComponent(module, out MetaDataComponent? metadata))
                    _rows.Add(module, BuildRow(module, component, metadata.EntityName));
            }
        }

        var canEject = suit.Comp.State == ModSuitState.Inactive && _wires.IsPanelOpen(suit.Owner);
        foreach (var (uid, row) in _rows)
        {
            if (!_entities.TryGetComponent(uid, out ModModuleComponent? module))
                continue;

            row.Remove.Disabled = !canEject || !module.Removable;
            if (row.Pin is { } pin)
            {
                var pinned = _entities.TryGetComponent(_players.LocalEntity, out ModAssistantComponent? assistant)
                    && assistant.Suit == suit.Owner ? assistant.PinnedModules.Contains(uid) : module.Pinned;
                pin.Text = Loc.GetString(pinned ? "modsuit-ui-module-unpin" : "modsuit-ui-module-pin");
            }
            if (row.Use is not { } use)
                continue;

            use.Text = Loc.GetString(module.Type == ModModuleType.Usable ? "modsuit-ui-module-use"
                : module.Active ? "modsuit-ui-module-off" : "modsuit-ui-module-on");
        }
    }

    private ModuleRow BuildRow(EntityUid uid, ModModuleComponent module, string name)
    {
        var net = _entities.GetNetEntity(uid);
        var row = new BoxContainer { SeparationOverride = 6 };
        var icon = new SpriteView { SetSize = new Vector2(32, 32) };
        icon.SetEntity(uid);
        row.AddChild(icon);
        row.AddChild(new Label { Text = name, HorizontalExpand = true, ClipText = true });

        Button? use = null;
        Button? pin = null;
        if (module.Type != ModModuleType.Passive)
        {
            use = new Button { MinWidth = 50 };
            use.OnPressed += _ => OnUseModule?.Invoke(net);
            row.AddChild(use);
            if (module.Action != null)
            {
                pin = new Button { MinWidth = 50 };
                pin.OnPressed += _ => OnPinModule?.Invoke(net);
                row.AddChild(pin);
            }
        }

        var remove = new Button { Text = Loc.GetString(module.Removable ? "modsuit-ui-module-remove" : "modsuit-ui-module-builtin") };
        remove.OnPressed += _ => OnRemoveModule?.Invoke(net);
        row.AddChild(remove);
        ModulesList.AddChild(row);
        return new ModuleRow(use, pin, remove);
    }

    private sealed record ModuleRow(Button? Use, Button? Pin, Button Remove);
}
