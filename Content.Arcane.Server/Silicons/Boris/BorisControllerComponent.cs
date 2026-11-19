using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.Silicons.Boris;

[RegisterComponent, Access(typeof(BorisSystem))]
public sealed partial class BorisControllerComponent : Component
{
    public EntityUid Board;
    public EntityUid? Ai;
    public EntityUid? Core;
    public EntityUid? Mind;
    public EntityUid? ReturnAction;
    public HashSet<ProtoId<RadioChannelPrototype>>? ReceiveChannels;
    public HashSet<ProtoId<RadioChannelPrototype>>? TransmitChannels;
}
