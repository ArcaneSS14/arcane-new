using System.Linq;
using Content.Shared.Humanoid;
using Content.Shared.TTS;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    public static readonly ProtoId<TTSVoicePrototype> DefaultTtsVoice = "Jackie";

    /// <summary>
    /// TTS voice used when the profile has none that fits its sex.
    /// </summary>
    public static readonly Dictionary<Sex, ProtoId<TTSVoicePrototype>> DefaultSexTtsVoice = new()
    {
        { Sex.Male, "Jackie" },
        { Sex.Female, "Lina_dota_2" },
        { Sex.Unsexed, "Lambert" },
        { Sex.Futanari, "Drow_ranger_dota_2" },
    };

    /// <summary>
    /// Text-to-speech voice of this character.
    /// </summary>
    [DataField]
    public ProtoId<TTSVoicePrototype> TtsVoice { get; private set; } = DefaultTtsVoice;

    public HumanoidCharacterProfile WithTtsVoice(ProtoId<TTSVoicePrototype> voice)
    {
        return new(this) { TtsVoice = voice };
    }

    /// <summary>
    /// Whether a character of this sex can pick the voice in the character editor.
    /// </summary>
    public static bool CanHaveTtsVoice(TTSVoicePrototype voice, Sex sex)
    {
        return voice.RoundStart
               && (sex == Sex.Unsexed
                   || voice.Sex == sex
                   || voice.Sex == Sex.Female && sex == Sex.Futanari
                   || voice.Sex == Sex.Unsexed);
    }

    public static ProtoId<TTSVoicePrototype> RandomTtsVoice(Sex sex, IPrototypeManager prototypes, IRobustRandom random)
    {
        var candidates = prototypes.EnumeratePrototypes<TTSVoicePrototype>()
            .Where(voice => CanHaveTtsVoice(voice, sex))
            .ToArray();

        return candidates.Length > 0 ? random.Pick(candidates).ID : DefaultSexTtsVoice.GetValueOrDefault(sex, DefaultTtsVoice);
    }

    /// <summary>
    /// The voice if it exists, otherwise the default voice of the sex.
    /// </summary>
    private ProtoId<TTSVoicePrototype> ValidatedTtsVoice(IPrototypeManager prototypes, Sex sex)
    {
        return prototypes.HasIndex(TtsVoice) ? TtsVoice : DefaultSexTtsVoice.GetValueOrDefault(sex, DefaultTtsVoice);
    }
}
