// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Barks;
using Content.Trauma.Common.Knowledge;
using Robust.Shared.Prototypes;
// Arcane-Start
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
// Arcane-End

namespace Content.Shared.Humanoid;

/// <summary>
/// Trauma - store the profile's bark voice and knowledge settings
/// </summary>
public sealed partial class HumanoidProfileComponent
{
    // Arcane-Start: Replicate character growth on live humanoids
    [DataField, AutoNetworkedField]
    public float Height = 1f;

    [DataField, AutoNetworkedField]
    public float Width = 1f;
    // Arcane-End

    [DataField]
    public ProtoId<BarkPrototype> BarkVoice = HumanoidProfileSystem.DefaultBarkVoice;

    [DataField]
    public KnowledgeProfile Knowledge = new();

    public Dictionary<string, float> BaseFixtureRadii = new(); // Arcane
}
