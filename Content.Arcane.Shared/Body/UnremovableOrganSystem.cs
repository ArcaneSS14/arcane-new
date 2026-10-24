// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;

namespace Content.Arcane.Shared.Body;

/// <summary>
/// Prevents organs carrying <see cref="UnremovableOrganComponent"/> from being removed.
/// </summary>
public sealed partial class UnremovableOrganSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnOrganRemoveAttempt(Entity<UnremovableOrganComponent> ent, ref OrganRemoveAttemptEvent args)
    {
        args.Cancelled = true;
    }
}
