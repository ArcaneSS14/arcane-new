using Content.Arcane.Shared.ModSuits;
using Content.Arcane.Shared.ModSuits.Components;
using Content.Trauma.Server.Atmos.Components;

namespace Content.Trauma.Server.Atmos.EntitySystems;

public sealed partial class ModPlasmaStabilizerSystem : EntitySystem
{
    [Dependency] private IgniteFromGasSystem _ignite = default!;
    private readonly Dictionary<EntityUid, IgniteFromGasImmunityComponent> _protection = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<ModPlasmaStabilizerModuleComponent, ModModuleActivatedEvent>(OnActivated);
        SubscribeLocalEvent<ModPlasmaStabilizerModuleComponent, ModModuleDeactivatedEvent>(OnDeactivated);
    }

    private void OnActivated(Entity<ModPlasmaStabilizerModuleComponent> module, ref ModModuleActivatedEvent args)
    {
        if (HasComp<IgniteFromGasImmunityComponent>(args.Suit))
            return;

        var protection = new IgniteFromGasImmunityComponent { Parts = ["Head"] };
        AddComp(args.Suit, protection);
        _protection.Add(module.Owner, protection);
        if (args.Wearer is { } wearer)
            _ignite.UpdateIgniteImmunity(wearer);
    }

    private void OnDeactivated(Entity<ModPlasmaStabilizerModuleComponent> module, ref ModModuleDeactivatedEvent args)
    {
        if (!_protection.Remove(module.Owner, out var protection))
            return;

        if (!TerminatingOrDeleted(args.Suit) && TryComp(args.Suit, out IgniteFromGasImmunityComponent? current)
            && ReferenceEquals(protection, current))
            RemComp(args.Suit, current);
        if (args.Wearer is { } wearer && !TerminatingOrDeleted(wearer))
            _ignite.UpdateIgniteImmunity(wearer);
    }
}
