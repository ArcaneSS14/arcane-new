// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Coordinates.Helpers;
using Content.Shared.Interaction;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Goobstation.Shared.Religion.Nullrod;

public sealed partial class NullrodTransformSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private INetManager _net = default!; // Arcane

    public static readonly ProtoId<TagPrototype> NullrodTag = "Nullrod";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AltarSourceComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid uid, AltarSourceComponent component, InteractUsingEvent args)
    {
        // Checks used entity for the tag we need.
        // Arcane-Edit-Start
        if (args.Handled || _net.IsClient || EntityManager.IsQueuedForDeletion(args.Used)
            || !_tag.HasTag(args.Used, NullrodTag))
        // Arcane-Edit-End
            return;

        // Arcane-Start
        var attempt = new NullrodTransformAttemptEvent(args.User);
        RaiseLocalEvent(args.Used, ref attempt);
        if (attempt.Cancelled)
        {
            args.Handled = true;
            return;
        }
        // Arcane-End

        // *flaaavor*
        // Arcane-Edit-Start
        SpawnAtPosition(component.EffectProto, Transform(uid).Coordinates);
        _audio.PlayPvs(component.SoundPath, uid, AudioParams.Default.WithVolume(-4f));
        // Arcane-Edit-End

        // Spawn proto associated with the altar.
        var replacement = SpawnAtPosition(component.RodProto, args.ClickLocation.SnapToGrid(EntityManager)); // Arcane-Edit: PredictedSpawnAtPosition > SpawnAtPosition

        // Arcane-Start
        var transformed = new NullrodTransformedEvent(args.User, replacement);
        RaiseLocalEvent(args.Used, ref transformed);
        // Arcane-End

        // Remove the nullrod
        QueueDel(args.Used); // Arcane-Edit: PredictedQueueDel > QueueDel
        args.Handled = true;
    }
}
