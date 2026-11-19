using Content.Shared.Emag.Systems;

namespace Content.Arcane.Shared.Silicon.Bots;

/// <summary>
/// Accepts a normal emag through the existing shared emag interaction.
/// The emag owner supplies feedback and the persistent, replicated marker.
/// </summary>
public sealed partial class PlantbotSystem : EntitySystem
{
    [Dependency] private EmagSystem _emag = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlantbotComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnEmagged(Entity<PlantbotComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction) ||
            _emag.CheckFlag(ent, EmagType.Interaction))
            return;

        args.Handled = true;
    }
}
