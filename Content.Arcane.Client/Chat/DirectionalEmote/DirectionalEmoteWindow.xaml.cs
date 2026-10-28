using Content.Client.UserInterface.Controls;

namespace Content.Arcane.Client.Chat.DirectionalEmote;

[GenerateTypedNameReferences]
public sealed partial class DirectionalEmoteWindow : FancyWindow
{
    public NetEntity Target { get; set; }
    public string Text { get; private set; } = string.Empty;

    public event Action? AcceptPressed;
    public event Action? MessageChanged;

    public DirectionalEmoteWindow()
    {
        RobustXamlLoader.Load(this);

        AcceptButton.OnPressed += _ => AcceptPressed?.Invoke();
        MessageInput.OnTextChanged += e =>
        {
            Text = Rope.Collapse(e.TextRope).Trim();
            MessageChanged?.Invoke();
        };
    }
}
