using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Arcane.Shared.WallStains.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WallStainComponent : Component
{
    [DataField]
    public string SolutionName = "stain";

    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxStainVolume = FixedPoint2.New(5);

    [DataField, AutoNetworkedField]
    public Color Color = Color.White;

    [DataField, AutoNetworkedField]
    public float FillLevel;

    [DataField, AutoNetworkedField]
    public Vector2i Direction;

    [DataField, AutoNetworkedField]
    public int SplatSeed;

    public float EvaporationAccumulator;
}
