using Content.Shared.Examine;
using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Spawners;

namespace Content.Client.DirectionalArrowIndicator;

/// <summary>
/// System responsible for handling <see cref="DirectionalArrowIndicatorComponent"/>s,
/// spawning directional arrow indicators clientside when an entity is examined.
/// </summary>
public sealed partial class DirectionalArrowIndicatorSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!; // Arcane-Edit: Mirror the indicator without rotating its transform.
    [Dependency] private SharedAppearanceSystem _appearance = default!; // Arcane

    private const float EdgeOffset = 0.78125f; // Used for moving arrow to the edge of the tile by default.

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DirectionalArrowIndicatorComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<DirectionalArrowIndicatorComponent> ent, ref ExaminedEvent args)
    {
        var arrows = ent.Comp.Arrows;
        var lifetime = ent.Comp.Lifetime;
        // Arcane-Start
        var flipped = ent.Comp.FlipOnAppearance != null &&
            _appearance.TryGetData<bool>(ent, ent.Comp.FlipOnAppearance, out var flip) && flip;
        foreach (var previous in ent.Comp.SpawnedArrows)
        {
            if (!TerminatingOrDeleted(previous))
                QueueDel(previous);
        }
        ent.Comp.SpawnedArrows.Clear();
        // Arcane-End

        if (arrows.Count == 0)
            arrows.Add(new ArrowSpawnData());

        foreach (var arrowData in ent.Comp.Arrows)
        {
            // Arcane-Edit-Start: Set the initial direction without interpolation; mirror arrows at their original ports.
            var spawnedEnt = SpawnAttachedTo(arrowData.ArrowType,
                new EntityCoordinates(ent, arrowData.Offset.X, arrowData.Offset.Y + EdgeOffset), rotation: arrowData.Rotation);
            if (flipped)
                _sprite.SetScale(spawnedEnt, new Vector2(1, -1));
            ent.Comp.SpawnedArrows.Add(spawnedEnt);
            // Arcane-Edit-End

            EnsureComp<TimedDespawnComponent>(spawnedEnt, out var timedDespawn);
            timedDespawn.Lifetime = lifetime;
        }
    }
}
