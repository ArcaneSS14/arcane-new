using Content.Trauma.Common.Language;
using Robust.Shared.Player;

namespace Content.Arcane.Shared.TTS;

[ByRefEvent]
public readonly record struct TTSRadioPlayEvent(string Message, LanguagePrototype Language, string Voice, int Frequency);

[ByRefEvent]
public readonly record struct TTSAnnouncePlayEvent(string Message, EntityUid? Sender, Filter Receievers);
