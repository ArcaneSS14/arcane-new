// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Arcane.Shared.ModSuits.Systems;

namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent, Access(typeof(ModSuitSystem))]
public sealed partial class ModHatStabilizerModuleComponent : Component
{
    public EntityUid? Helmet;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ModHatHolderComponent : Component
{
    public const string SlotId = "mod-hat";
}
