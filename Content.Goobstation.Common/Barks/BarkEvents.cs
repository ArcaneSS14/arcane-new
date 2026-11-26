// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Common.Barks;

[Serializable, NetSerializable]
public sealed class PlayBarkEvent(NetEntity sourceUid, string message, bool whisper) : EntityEventArgs
{
    public NetEntity SourceUid { get; } = sourceUid;
    public string Message { get; } = message;
    public bool Whisper { get; } = whisper;
}

[Serializable, NetSerializable]
public sealed class PreviewBarkEvent(string barkProtoID) : EntityEventArgs
{
    public string BarkProtoID { get; } = barkProtoID;
}

// Arcane-Start: Radio listeners may not have the speaker in their PVS.
[Serializable, NetSerializable]
public sealed class PlayRadioSpeechBarkEvent(string voice, string message) : EntityEventArgs
{
    public string Voice { get; } = voice;
    public string Message { get; } = message;
}
// Arcane-End
