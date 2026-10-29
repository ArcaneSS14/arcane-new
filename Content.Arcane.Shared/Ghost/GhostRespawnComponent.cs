using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Arcane.Shared.Ghost;

/// <summary>
/// When this ghost may return to the round through the lobby (Orion #54, #253, #261, #389, #396).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GhostRespawnComponent : Component
{
    /// <summary>
    /// Game time (CurTime) at which returning is allowed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan RespawnAt;
}

[Serializable, NetSerializable]
public sealed class GhostReturnToRoundRequestEvent : EntityEventArgs;
