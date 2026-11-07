using Content.Arcane.Common.Camera;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;

namespace Content.Arcane.Shared.Camera.Trigger;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ArcaneScreenshakeUserOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField]
    public ArcaneScreenshakeParameters? Translation;

    [DataField]
    public ArcaneScreenshakeParameters? Rotation;
}
