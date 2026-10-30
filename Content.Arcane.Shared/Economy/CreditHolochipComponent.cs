using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Arcane.Shared.Economy;

/// <summary>
///     Credit stack shown as a holochip, coloured by its amount. EMPs can wipe it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CreditHolochipComponent : Component;

[Serializable, NetSerializable]
public enum CreditHolochipVisuals : byte
{
    BaseState,
    OverlayState,
    BaseColor,
}
