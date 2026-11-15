// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.EntityEffects;

/// <summary>Allows an entity to defer handling a matched reactive effect.</summary>
[ByRefEvent]
public record struct ReactiveEffectsApplyingEvent(EntityEffect[] Effects, float Scale)
{
    public bool Handled;
}
