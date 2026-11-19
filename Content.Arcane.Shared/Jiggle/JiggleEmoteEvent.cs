// Adapted from Goob-Station #7422 by Aiden, licensed under AGPL-3.0-or-later.

using System.Numerics;
using Content.Trauma.Common.EntityEffects;
using Robust.Shared.Serialization;

namespace Content.Arcane.Shared.Jiggle;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class JiggleEmoteEvent : EntityEffectNetworkEvent
{
    [DataField(required: true)]
    public List<JigglePulse> Pulses = new();
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class JigglePulse
{
    [DataField]
    public float Delay;

    [DataField(required: true)]
    public Vector2 Impulse;
}
