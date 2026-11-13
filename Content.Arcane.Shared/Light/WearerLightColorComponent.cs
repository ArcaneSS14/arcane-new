using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Shared.Light;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class WearerLightColorComponent : Component
{
    [DataField]
    public ProtoId<SpeciesPrototype>? Species = "Plasmaman";

    [DataField, AutoNetworkedField]
    public Color? DefaultColor;

    [AutoNetworkedField]
    public bool LightSuppressed;
}
