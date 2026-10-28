// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Arcane.Common.Preferences;
using Content.Shared.Eui;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Enums;

namespace Content.Arcane.Shared.DetailExaminable;

/// <summary>
/// Everything the detail examine window shows about a character.
/// NSFW sections are already removed by the server when the viewer must not see them.
/// </summary>
[Serializable, NetSerializable]
public sealed class DetailExaminableEuiState(
    NetEntity target,
    string name,
    ProtoId<SpeciesPrototype> species,
    Sex sex,
    Gender gender,
    string flavorText,
    ExtendedFlavorText extended,
    bool showNsfw) : EuiStateBase
{
    public readonly NetEntity Target = target;
    public readonly string Name = name;
    public readonly ProtoId<SpeciesPrototype> Species = species;
    public readonly Sex Sex = sex;
    public readonly Gender Gender = gender;
    public readonly string FlavorText = flavorText;
    public readonly ExtendedFlavorText Extended = extended;
    public readonly bool ShowNsfw = showNsfw;
}
