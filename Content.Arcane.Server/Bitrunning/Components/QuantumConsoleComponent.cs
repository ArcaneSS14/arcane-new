using Content.Arcane.Server.Bitrunning.Systems;

namespace Content.Arcane.Server.Bitrunning.Components;

[RegisterComponent]
public sealed partial class QuantumConsoleComponent : Component
{
    [Access(typeof(QuantumConsoleSystem))]
    public EntityUid? LinkedServerId;
}
