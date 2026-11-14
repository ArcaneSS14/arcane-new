// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Map;

namespace Content.Arcane.Shared.ModSuits;

public sealed partial class ModPowerKickActionEvent : WorldTargetActionEvent;

[Serializable, NetSerializable]
public sealed partial class ModPowerKickDoAfterEvent : DoAfterEvent
{
    [DataField]
    public NetCoordinates Destination { get; private set; }

    [DataField]
    public NetEntity? KickTarget { get; private set; }

    private ModPowerKickDoAfterEvent()
    {
    }

    public ModPowerKickDoAfterEvent(NetCoordinates destination, NetEntity? target)
    {
        Destination = destination;
        KickTarget = target;
    }

    public override DoAfterEvent Clone() => new ModPowerKickDoAfterEvent(Destination, KickTarget);
}
