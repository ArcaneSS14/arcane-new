// SPDX-FileCopyrightText: 2024 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 JohnOakman <sremy2012@hotmail.fr>
// SPDX-FileCopyrightText: 2025 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Arcane.Shared.Harpy;

[RegisterComponent]
public sealed partial class HarpySingerComponent : Component
{
    [DataField("midiActionId", serverOnly: true)]
    public EntProtoId? MidiActionId = "ActionHarpyPlayMidi";

    // The action entity carries a server-side BUI event and stays on the server.
    [DataField("midiAction", serverOnly: true)]
    public EntityUid? MidiAction;

    [DataField("ShutUpDamageThreshold", serverOnly: true)]
    public int? ShutUpDamageThreshold;
}
