namespace Content.Shared.DeviceLinking.Events
{
    public sealed class PortDisconnectedEvent : EntityEventArgs
    {
        public readonly string Port;
        public readonly EntityUid? RemovedPortUid; // Arcane

        public PortDisconnectedEvent(string port, EntityUid? removedPortUid = null) // Arcane-Edit: added removedPortUid
        {
            Port = port;
            RemovedPortUid = removedPortUid; // Arcane
        }
    }
}
