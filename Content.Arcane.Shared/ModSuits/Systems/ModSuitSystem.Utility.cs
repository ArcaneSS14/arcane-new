using Content.Arcane.Common.ModSuits;
using Content.Arcane.Shared.ModSuits.Components;
using Content.Goobstation.Shared.Loudspeaker.Components;
using Content.Shared.Instruments;
using Content.Shared.Inventory;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IRobustRandom _utilityRandom = default!;
    [Dependency] private ActivatableUISystem _activatableUi = default!;
    [Dependency] private SharedInstrumentSystem _instruments = default!;

    private void InitializeUtilityModules()
    {
        SubscribeLocalEvent<ModLoudspeakerModuleComponent, ModModuleActivatedEvent>(OnLoudspeakerActivated);
        SubscribeLocalEvent<ModLoudspeakerModuleComponent, ModModuleDeactivatedEvent>(OnLoudspeakerDeactivated);
        SubscribeLocalEvent<ModLoudspeakerModuleComponent, EntityTerminatingEvent>(OnLoudspeakerTerminating);
        SubscribeLocalEvent<ModSoundModuleComponent, ModModuleUsedEvent>(OnSoundModuleUsed);
        SubscribeLocalEvent<ModSpawnModuleComponent, ModModuleUsedEvent>(OnSpawnModuleUsed);
        SubscribeLocalEvent<ModInstrumentModuleComponent, ModModuleUsedEvent>(OnInstrumentModuleUsed);
        SubscribeLocalEvent<InventoryComponent, ModifyGrabEvent>(OnModifyModGrab);
    }

    private void OnInstrumentModuleUsed(Entity<ModInstrumentModuleComponent> module, ref ModModuleUsedEvent args)
    {
        if (args.Handled || GetDeployedPart(args.Suit, "head") is not { } helmet
            || !HasModInstrument(helmet)
            || !TryComp(helmet, out ActivatableUIComponent? instrumentUi))
            return;

        args.Handled = _activatableUi.InteractUI(args.User, helmet, instrumentUi, InstrumentUiKey.Key);
    }

    private bool HasModInstrument(EntityUid helmet)
    {
        SharedInstrumentComponent? instrument = null;
        return _instruments.ResolveInstrument(helmet, ref instrument);
    }

    private void OnModifyModGrab(Entity<InventoryComponent> wearer, ref ModifyGrabEvent args)
    {
        if (_hands.TryGetActiveItem(wearer.Owner, out var held)
            && !HasComp<VirtualItemComponent>(held))
            return;

        if (!_inventory.TryGetSlotEntity(wearer, "gloves", out var gloves, wearer.Comp)
            || !HasComp<ModGripComponent>(gloves))
            return;

        args.UpgradeSoft = true;
        args.EscapeMultiplier *= 0.8f;
        args.EscapeOffset -= 0.1f;
        args.SpeedMultiplier *= 2f;
    }

    private void OnLoudspeakerActivated(Entity<ModLoudspeakerModuleComponent> module, ref ModModuleActivatedEvent args)
    {
        if (args.Wearer is not { } wearer || TerminatingOrDeleted(wearer) || module.Comp.RegisteredWearer == wearer)
            return;

        UnregisterLoudspeaker(module);
        EnsureComp<LoudspeakerHolderComponent>(wearer).Loudspeakers.Add(module);
        module.Comp.RegisteredWearer = wearer;
    }

    private void OnLoudspeakerDeactivated(Entity<ModLoudspeakerModuleComponent> module, ref ModModuleDeactivatedEvent args) =>
        UnregisterLoudspeaker(module);

    private void OnLoudspeakerTerminating(Entity<ModLoudspeakerModuleComponent> module, ref EntityTerminatingEvent args) =>
        UnregisterLoudspeaker(module);

    private void UnregisterLoudspeaker(Entity<ModLoudspeakerModuleComponent> module)
    {
        if (module.Comp.RegisteredWearer is not { } wearer)
            return;

        module.Comp.RegisteredWearer = null;
        if (TerminatingOrDeleted(wearer) || !TryComp(wearer, out LoudspeakerHolderComponent? holder))
            return;

        holder.Loudspeakers.Remove(module);
        if (holder.Loudspeakers.Count == 0)
            RemComp(wearer, holder);
    }

    private void OnSoundModuleUsed(Entity<ModSoundModuleComponent> module, ref ModModuleUsedEvent args)
    {
        if (args.Handled)
            return;

        _audio.PlayPvs(module.Comp.Sound, args.User);
        args.Handled = true;
    }

    private void OnSpawnModuleUsed(Entity<ModSpawnModuleComponent> module, ref ModModuleUsedEvent args)
    {
        if (_net.IsClient || args.Handled || module.Comp.Prototypes.Count == 0)
            return;

        var item = SpawnAtPosition(_utilityRandom.Pick(module.Comp.Prototypes), Transform(args.User).Coordinates);
        if (module.Comp.ToHand)
            _hands.PickupOrDrop(args.User, item);
        args.Handled = true;
    }
}
