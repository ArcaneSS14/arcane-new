using System.Diagnostics.CodeAnalysis;
using Content.Arcane.Shared.Paper;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Arcane.Client.Paper;

public sealed class PaperFormTag : IMarkupTagHandler
{
    public string Name => "form";
    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
        => PaperFieldTags.Create(node, PaperFieldKind.Form, Loc.GetString("paper-field-fill"), out control);
}

public sealed class PaperCheckTag : IMarkupTagHandler
{
    public string Name => "check";
    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
        => PaperFieldTags.Create(node, PaperFieldKind.Check, "☐", out control);
}

public sealed class PaperSignatureTag : IMarkupTagHandler
{
    public string Name => "signature";
    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
        => PaperFieldTags.Create(node, PaperFieldKind.Signature, Loc.GetString("paper-field-sign"), out control);
}

internal static class PaperFieldTags
{
    public static bool Create(MarkupNode node, PaperFieldKind kind, string text, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        if (node.Value != default || node.Attributes.Count != 2 ||
            !node.Attributes.TryGetValue(PaperFields.IndexAttribute, out var indexValue) ||
            !indexValue.TryGetLong(out var index) || index < 0 || index > int.MaxValue ||
            !node.Attributes.TryGetValue(PaperFields.LockedAttribute, out var lockedValue) ||
            !lockedValue.TryGetLong(out var locked))
            return false;

        var button = new Button { Text = text, Disabled = locked != 0 };
        button.OnPressed += _ =>
        {
            for (var parent = button.Parent; parent != null; parent = parent.Parent)
            {
                if (parent is not ArcanePaperWindow paper)
                    continue;
                paper.OpenField(kind, (int) index);
                break;
            }
        };
        control = button;
        return true;
    }
}
