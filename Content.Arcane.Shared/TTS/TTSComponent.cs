using Content.Shared.TTS;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Shared.TTS;

[RegisterComponent, NetworkedComponent]
public sealed partial class TTSComponent : Component
{
    [DataField("voice")]
    public ProtoId<TTSVoicePrototype>? VoicePrototype = "Gman";

    [DataField]
    public string? Effect;
}
