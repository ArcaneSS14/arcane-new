using Content.Arcane.Shared.ModSuits.Components;
using Content.Goobstation.Shared.Loudspeaker.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IRobustRandom _utilityRandom = default!;

    private void InitializeUtilityModules()
    {
        SubscribeLocalEvent<ModLoudspeakerModuleComponent, ModModuleActivatedEvent>(OnLoudspeakerActivated);
        SubscribeLocalEvent<ModLoudspeakerModuleComponent, ModModuleDeactivatedEvent>(OnLoudspeakerDeactivated);
        SubscribeLocalEvent<ModLoudspeakerModuleComponent, EntityTerminatingEvent>(OnLoudspeakerTerminating);
        SubscribeLocalEvent<ModSoundModuleComponent, ModModuleUsedEvent>(OnSoundModuleUsed);
        SubscribeLocalEvent<ModSpawnModuleComponent, ModModuleUsedEvent>(OnSpawnModuleUsed);
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
