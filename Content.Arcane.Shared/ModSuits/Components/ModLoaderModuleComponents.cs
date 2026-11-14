// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Map;

namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent]
public sealed partial class ModLoaderMagnetComponent : Component
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public float PickRange = 0.75f;

    [DataField]
    public float PullSpeed = 8f;

    [DataField]
    public float LaunchRange = 7f;

    [DataField]
    public float LaunchSpeed = 12f;

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_Arcane/Items/Modsuit/gravhit.ogg");
}

[RegisterComponent]
public sealed partial class ModLoaderMagnetizedComponent : Component
{
    public EntityUid Puller;
    public EntityUid Module;
}

public sealed partial class ModLoaderMagnetActionEvent : WorldTargetActionEvent;

[RegisterComponent]
public sealed partial class ModCandelaModuleComponent : Component
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public float Spacing = 6f;

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/thunk.ogg");

    public MapCoordinates? LastBeacon;
    public bool WarnedEmpty;
}
