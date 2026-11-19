using Content.Arcane.Shared.Paper;
using Content.Client.Paper.UI;
using Content.Client.RichText;
using Content.Shared.Paper;
using Robust.Shared.Utility;

namespace Content.Arcane.Client.Paper;

public sealed class ArcanePaperWindow : PaperWindow
{
    private static readonly Type[] PaperTags =
    [
        ..UserFormattableTags.BaseAllowedTags,
        typeof(PaperFormTag), typeof(PaperCheckTag), typeof(PaperSignatureTag),
    ];

    private PaperComponent.PaperBoundUserInterfaceState? _state;
    private PaperFieldDialog? _dialog;
    private readonly Dictionary<(PaperFieldKind Kind, int Index), string> _fieldDrafts = new();
    public event Action<PaperFieldKind, int, string, string>? OnFieldSubmitted;

    public ArcanePaperWindow()
    {
        Input.ToolTip = Loc.GetString("paper-field-help");
        OnClose += CloseDialog;
    }

    protected override Type[] AllowedMarkupTags => PaperTags;

    protected override FormattedMessage FormatPaperText(PaperComponent.PaperBoundUserInterfaceState state)
        // Advanced pens can write after stamping; the server checks the current tool.
        => PaperFields.ForDisplay(state.Text, false);

    protected override StampWidget CreateStampWidget(StampDisplayInfo info) => new ArcaneStampWidget { StampInfo = info };

    public override void Populate(PaperComponent.PaperBoundUserInterfaceState state)
    {
        if (_state?.Text != state.Text)
        {
            CloseDialog();
            _fieldDrafts.Clear();
        }
        _state = state;
        base.Populate(state);
    }

    public void OpenField(PaperFieldKind kind, int index)
    {
        if (_state is not { } state)
            return;
        CloseDialog();
        if (kind == PaperFieldKind.Signature)
        {
            OnFieldSubmitted?.Invoke(kind, index, string.Empty, state.Text);
            return;
        }

        var dialog = new PaperFieldDialog(kind, MaxInputLength);
        if (_fieldDrafts.TryGetValue((kind, index), out var draft))
            dialog.Value = draft;
        _dialog = dialog;
        dialog.OnSubmitted += value =>
        {
            OnFieldSubmitted?.Invoke(kind, index, value, state.Text);
        };
        dialog.OnClose += () =>
        {
            if (_state?.Text == state.Text && kind == PaperFieldKind.Form)
                _fieldDrafts[(kind, index)] = dialog.Value;
            if (_dialog == dialog)
                _dialog = null;
        };
        dialog.OpenCentered();
    }

    private void CloseDialog() => _dialog?.Close();

    protected override void ExitedTree()
    {
        CloseDialog();
        _fieldDrafts.Clear();
        _state = null;
        OnFieldSubmitted = null;
        base.ExitedTree();
    }
}
