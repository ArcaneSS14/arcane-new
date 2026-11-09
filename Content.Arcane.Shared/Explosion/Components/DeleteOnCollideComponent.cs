using Content.Arcane.Shared.Explosion.Systems;
using Robust.Shared.GameStates;

namespace Content.Arcane.Shared.Explosion.Components;

/// <summary>
/// Deletes this effect entity after it starts colliding with another physics body.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ExplosionProjectileCollisionSystem))]
public sealed partial class DeleteOnCollideComponent : Component;
