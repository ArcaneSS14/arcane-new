using System.Text;
using Robust.Shared.Utility;

namespace Content.Arcane.Shared.Paper;

/// <summary>Document-local field indices using the same markup parser as the paper renderer.</summary>
public static class PaperFields
{
    public const string IndexAttribute = "paperindex";
    public const string LockedAttribute = "paperlocked";

    public static FormattedMessage ForDisplay(string content, bool locked)
    {
        var message = FormattedMessage.FromMarkupPermissive(content);
        var indices = new int[3];
        var display = new StringBuilder();
        foreach (var node in message)
        {
            if (!TryGetKind(node, out var kind))
            {
                // Author-provided attributes must never impersonate renderer-owned field indices.
                display.Append(node.Name is "form" or "check" or "signature"
                    ? FormattedMessage.EscapeText(node.ToString())
                    : node.ToString());
                continue;
            }

            node.Attributes[IndexAttribute] = new MarkupParameter(indices[(int) kind]++);
            node.Attributes[LockedAttribute] = new MarkupParameter(locked ? 1 : 0);
            display.Append(node.ToString());
        }
        return FormattedMessage.FromMarkupPermissive(display.ToString());
    }

    public static bool TryFill(string content, PaperFieldKind kind, int index, string value, int contentSize, out string result)
    {
        result = content;
        if (!Enum.IsDefined(kind) || index < 0 || contentSize < 0 || content.Length > contentSize ||
            value.Length > contentSize || string.IsNullOrEmpty(value) ||
            kind == PaperFieldKind.Check && value is not ("☐" or "✔" or "✖"))
            return false;

        var message = FormattedMessage.FromMarkupPermissive(content);
        MarkupNode? target = null;
        var count = 0;
        foreach (var node in message)
        {
            if (TryGetKind(node, out var foundKind) && foundKind == kind && count++ == index)
            {
                target = node;
                break;
            }
        }
        if (target == null)
            return false;

        var text = new StringBuilder();
        foreach (var node in message)
        {
            text.Append(ReferenceEquals(node, target) ? FormattedMessage.EscapeText(value) : node.ToString());
            if (text.Length > contentSize)
                return false;
        }
        result = text.ToString();
        return true;
    }

    public static string FinalizeDocument(string content)
    {
        var message = FormattedMessage.FromMarkupPermissive(content);
        var text = new StringBuilder();
        var changed = false;
        foreach (var node in message)
        {
            if (TryGetKind(node, out var kind))
            {
                if (kind == PaperFieldKind.Check)
                    text.Append('☐');
                changed = true;
            }
            else
                text.Append(node.ToString());
        }
        return changed ? text.ToString() : content;
    }

    private static bool TryGetKind(MarkupNode node, out PaperFieldKind kind)
    {
        kind = default;
        if (node.Closing || node.Attributes.Count != 0 || node.Value != default)
            return false;
        switch (node.Name)
        {
            case "form": kind = PaperFieldKind.Form; return true;
            case "check": kind = PaperFieldKind.Check; return true;
            case "signature": kind = PaperFieldKind.Signature; return true;
            default: return false;
        }
    }
}
