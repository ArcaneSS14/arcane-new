using Content.Shared.Implants.Components;
using Content.Shared.Trigger.Components.Effects; // Arcane
using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerOnActivateImplantSystem : TriggerOnXSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnActivateImplantComponent, ActivateImplantEvent>(OnActivateImplant);
    }

    private void OnActivateImplant(Entity<TriggerOnActivateImplantComponent> ent, ref ActivateImplantEvent args)
    {
        var triggered = Trigger.Trigger(ent.Owner, args.Performer, ent.Comp.KeyOut);
        args.Handled |= triggered || !HasComp<ScramOnTriggerComponent>(ent); // Arcane: SCRAM spends a charge only after moving.
    }
}
