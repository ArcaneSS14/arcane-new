using Robust.Shared.Serialization;

namespace Content.Arcane.Shared.Paper;

[Serializable, NetSerializable]
public enum PaperFieldKind : byte
{
    Form,
    Check,
    Signature,
}

/// <summary>Fill one field in the document the reader actually saw. Signatures are supplied by the server.</summary>
[Serializable, NetSerializable]
public sealed class PaperFieldMessage : BoundUserInterfaceMessage
{
    public readonly PaperFieldKind Kind;
    public readonly int Index;
    public readonly string Value;
    public readonly string ExpectedContent;

    public PaperFieldMessage(PaperFieldKind kind, int index, string value, string expectedContent)
    {
        Kind = kind;
        Index = index;
        Value = value;
        ExpectedContent = expectedContent;
    }
}
