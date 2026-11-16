using Content.Arcane.Server.InvisibilityHeart.Components;
using Content.Shared.Body;
using Content.Shared.Ghost;
using Content.Arcane.Shared.Invisibility;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Arcane.Server.Invisibility;

/// <summary>
///     Fires once when an arcane invisibility ghost role finally spawns in. The body is fully
///     assembled by this point, so we can swap out the ordinary heart for the arcane one that
///     grants <see cref="ArcaneInvisibilityComponent"/> via its <c>onAdd</c>, then play the
///     materialization effect.
/// </summary>
public sealed partial class ArcaneArrivalSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly EntProtoId AppearEffect = "EffectTeleportationShort";

    private static readonly SoundSpecifier AppearSound =
        new SoundPathSpecifier(new ResPath("/Audio/_Goobstation/Effects/Slasher/SlasherJauntAppear.ogg"));

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostRoleSpawnerUsedEvent>(OnSpawnerUsed);
    }

    private void OnSpawnerUsed(ref GhostRoleSpawnerUsedEvent args)
    {
        if (!TryComp<InvisibilityHeartComponent>(args.Spawned, out var marker))
            return;

        ImplantHeart(args.Spawned, marker.OrganProto, marker.OrganSlot);

        // Defense in depth: even if the implant somehow missed the slot, an expanding
        // arrival should still be invisible. Cutting the heart out removes the component.
        EnsureComp<ArcaneInvisibilityComponent>(args.Spawned);

        Spawn(AppearEffect, _transform.GetMapCoordinates(args.Spawned));
        _audio.PlayPvs(AppearSound, args.Spawned);
    }

    private void ImplantHeart(EntityUid bodyId, EntProtoId organProto, string slotId)
    {
        // The organ's category picks the slot; the old heart is destroyed, not dropped.
        var organ = Spawn(organProto);
        if (Comp<OrganComponent>(organ).Category is { } category && _body.GetOrgan(bodyId, category) is { } old)
        {
            _body.RemoveOrgan(bodyId, old);
            QueueDel(old);
        }

        if (!_body.InsertOrgan(bodyId, organ))
            QueueDel(organ);
    }
}
