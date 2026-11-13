// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio; // Arcane

namespace Content.Goobstation.Shared.StationRadio.Events;

/// <summary>
/// Raised on a vinyl player when a vinyl is inserted and starts playing.
/// </summary>
[ByRefEvent]
public record struct VinylInsertedEvent(EntityUid Vinyl, ResolvedSoundSpecifier Track); // Arcane-Edit: add selected track

/// <summary>
/// Raised on a vinyl player when a vinyl is removed.
/// </summary>
[ByRefEvent]
public record struct VinylRemovedEvent(EntityUid Vinyl);
