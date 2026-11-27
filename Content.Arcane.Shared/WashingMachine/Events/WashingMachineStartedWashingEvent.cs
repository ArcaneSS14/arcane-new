namespace Content.Arcane.Shared.WashingMachine.Events;

public sealed partial class WashingMachineStartedWashingEvent : EntityEventArgs
{
    public HashSet<EntityUid> Items;

    public WashingMachineStartedWashingEvent(HashSet<EntityUid> items)
    {
        Items = items;
    }
}
