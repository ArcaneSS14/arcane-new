using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Tag;
using Content.Shared.Wall;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Arcane.Shared.ErpPanel.Requirements;

[Serializable, NetSerializable]
public sealed partial class NearbyStaticEntityRequirement : InvertableErpRequirement
{
    [DataField]
    public float Range = 0.5f;

    [DataField]
    public HashSet<ProtoId<TagPrototype>> Tags = new();

    /// <summary>
    /// Walls count too (they are marked with <see cref="WallComponent"/>, not a tag).
    /// </summary>
    [DataField]
    public bool Walls;

    public override bool IsAvailable(EntityUid uid, IEntityManager entityManager)
    {
        var lookupSystem = entityManager.System<EntityLookupSystem>();
        var containerSystem = entityManager.System<SharedContainerSystem>();
        var tagSystem = entityManager.System<TagSystem>();

        var nearbyEntities = lookupSystem.GetEntitiesInRange(uid, Range).ToList();

        var hasNearbyStatic = false;

        foreach (var entity in nearbyEntities)
        {
            if (entity == uid)
                continue;

            if (containerSystem.IsEntityInContainer(entity))
                continue;

            if (entityManager.TryGetComponent<PhysicsComponent>(entity, out var physics) &&
                physics.BodyType == BodyType.Static)
            {
                if (!(Walls && entityManager.HasComponent<WallComponent>(entity)) && !tagSystem.HasAnyTag(entity, Tags))
                    continue;

                hasNearbyStatic = true;
                break;
            }
        }

        return Inverted ? !hasNearbyStatic : hasNearbyStatic;
    }
}
