using Content.Arcane.Server.Gravity.Systems;

namespace Content.Arcane.Server.Gravity.Components;

[RegisterComponent]
[Access(typeof(GravitySourceSystem))]
public sealed partial class GravitySourceComponent : Component
{
    [ViewVariables]
    public bool Active;
}
