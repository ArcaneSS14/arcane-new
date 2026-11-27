using Content.Shared.Actions;
using Robust.Shared.Audio;

namespace Content.Arcane.Shared.Teleportation;

public sealed partial class ScramImplantEvent : InstantActionEvent
{
    [DataField]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");
}
