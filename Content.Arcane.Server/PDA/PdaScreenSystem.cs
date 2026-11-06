using Content.Arcane.Common.PDA;
using Content.Arcane.Shared.PDA;
using Content.Shared.CartridgeLoader;
using Content.Shared.PDA;

namespace Content.Arcane.Server.PDA;

/// <summary>
/// Keeps the PDA screen in sync with its interface and active program, and handles the power-off button.
/// </summary>
public sealed partial class PdaScreenSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private CartridgeLoaderSystem _cartridgeLoader = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PdaScreenComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PdaScreenComponent, BoundUIOpenedEvent>(OnUiChanged);
        SubscribeLocalEvent<PdaScreenComponent, BoundUIClosedEvent>(OnUiChanged);
        SubscribeLocalEvent<PdaScreenComponent, PdaPowerOffMessage>(OnPowerOff);
        SubscribeLocalEvent<CartridgeComponent, CartridgeActivatedEvent>(OnProgramActivated);
        SubscribeLocalEvent<CartridgeComponent, CartridgeDeactivatedEvent>(OnProgramDeactivated);
    }

    private void OnMapInit(Entity<PdaScreenComponent> ent, ref MapInitEvent args)
    {
        SetScreen(ent, ent.Comp.OffState);
    }

    private void OnUiChanged(Entity<PdaScreenComponent> ent, ref BoundUIOpenedEvent args)
    {
        Update(ent, null);
    }

    private void OnUiChanged(Entity<PdaScreenComponent> ent, ref BoundUIClosedEvent args)
    {
        Update(ent, null);
    }

    private void OnPowerOff(Entity<PdaScreenComponent> ent, ref PdaPowerOffMessage args)
    {
        if (!PdaUiKey.Key.Equals(args.UiKey))
            return;

        if (TryComp<CartridgeLoaderComponent>(ent, out var loader) && loader.ActiveProgram is { } program)
            _cartridgeLoader.DeactivateProgram((ent, loader), program);

        _ui.CloseUi(ent.Owner, PdaUiKey.Key, args.Actor);
        SetScreen(ent, ent.Comp.OffState);
    }

    private void OnProgramActivated(Entity<CartridgeComponent> ent, ref CartridgeActivatedEvent args)
    {
        if (TryComp<PdaScreenComponent>(args.Loader, out var screen))
            Update((args.Loader, screen), ent.Owner);
    }

    private void OnProgramDeactivated(Entity<CartridgeComponent> ent, ref CartridgeDeactivatedEvent args)
    {
        if (TryComp<PdaScreenComponent>(args.Loader, out var screen))
            Update((args.Loader, screen), EntityUid.Invalid);
    }

    /// <param name="program">The active program, <see cref="EntityUid.Invalid"/> for none, or null to read it from the loader.</param>
    private void Update(Entity<PdaScreenComponent> ent, EntityUid? program)
    {
        program ??= CompOrNull<CartridgeLoaderComponent>(ent)?.ActiveProgram ?? EntityUid.Invalid;

        string state;
        if (program.Value.IsValid())
        {
            var proto = MetaData(program.Value).EntityPrototype?.ID;
            state = proto != null && ent.Comp.ProgramScreens.TryGetValue(proto, out var programState)
                ? programState
                : ent.Comp.ProgramState;
        }
        else
        {
            state = _ui.IsUiOpen(ent.Owner, PdaUiKey.Key) ? ent.Comp.MenuState : ent.Comp.OffState;
        }

        SetScreen(ent, state);
    }

    private void SetScreen(EntityUid uid, string state)
    {
        _appearance.SetData(uid, PdaScreenVisuals.State, state);
    }
}
