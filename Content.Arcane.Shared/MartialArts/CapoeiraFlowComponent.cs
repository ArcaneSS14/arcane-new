namespace Content.Arcane.Shared.MartialArts;

/// <summary>
/// Goes on a martial art to make attacks flashy: strikes are somersaults and shoves spin the user.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(CapoeiraFlowSystem))]
public sealed partial class CapoeiraFlowComponent : Component;
