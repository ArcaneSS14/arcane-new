using Content.Shared.Interaction.Events;
using Content.Shared.Mech.Components;
using Content.Shared.Weapons.Ranged.Events; // Arcane

namespace Content.Shared.Mech.EntitySystems;

public abstract partial class SharedMechSystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<MechComponent, GettingAttackedAttemptEvent>(RelayRefToPilot);
        SubscribeLocalEvent<MechComponent, ShotAttemptedEvent>(RelayRefToPilot); // Arcane - let pilot-specific ranged restrictions apply.
    }

    private void RelayToPilot<T>(Entity<MechComponent> uid, T args) where T : class
    {
        if (uid.Comp.PilotSlot?.ContainedEntity is not { } pilot)
            return;

        var ev = new MechPilotRelayedEvent<T>(args);

        RaiseLocalEvent(pilot, ref ev);
    }

    private void RelayRefToPilot<T>(Entity<MechComponent> uid, ref T args) where T :struct
    {
        if (uid.Comp.PilotSlot?.ContainedEntity is not { } pilot)
            return;

        var ev = new MechPilotRelayedEvent<T>(args);

        RaiseLocalEvent(pilot, ref ev);

        args = ev.Args;
    }
}

[ByRefEvent]
public record struct MechPilotRelayedEvent<TEvent>(TEvent Args)
{
    public TEvent Args = Args;
}
