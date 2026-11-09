using Robust.Shared.Serialization;

namespace Content.Shared.PDA
{
    [Serializable, NetSerializable]
    public enum PdaVisuals
    {
        IdCardInserted,
        PdaType, // Arcane-Edit: PdaType > PdaType,
        PenInserted // Arcane
    }

    [Serializable, NetSerializable]
    public enum PdaUiKey
    {
        Key
    }

}
