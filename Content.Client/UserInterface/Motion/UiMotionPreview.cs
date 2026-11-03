// Arcane-Start
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Motion;

/// <summary>Exercises the saved window, button, checkbox and speech preferences without changing configuration.</summary>
public sealed partial class UiMotionPreview : FancyWindow
{
    [Dependency] private IConfigurationManager _configuration = default!;
    private readonly RichTextLabel _speech;
    private readonly UiTextReveal _reveal;
    private float _elapsed;
    private int _visible = -1;

    public UiMotionPreview()
    {
        IoCManager.InjectDependencies(this);
        Title = Loc.GetString("ui-motion-preview-title");
        MinWidth = 360;
        var body = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
        ContentsContainer.AddChild(body);
        for (var i = 1; i <= 6; i++)
            body.AddChild(new Button { Text = Loc.GetString("ui-motion-preview-button", ("number", i)) });
        body.AddChild(new CheckBox { Text = Loc.GetString("ui-motion-preview-checkbox") });
        _speech = new RichTextLabel { MaxWidth = 340 };
        body.AddChild(_speech);
        _reveal = new UiTextReveal(FormattedMessage.FromUnformatted(Loc.GetString("ui-motion-preview-speech")));
        _speech.SetMessage(_reveal.Reveal(0));
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        _elapsed += args.DeltaSeconds;
        var speed = UiMotionSettings.NormalizeSpeed(_configuration.GetCVar(UiMotionSettings.Speed));
        var pause = _configuration.GetCVar(UiMotionSettings.SpeechPunctuation)
            ? UiMotionSettings.Read(_configuration, UiMotionSettings.SpeechPunctuationMs, 0, 500) / 1000 / speed : 0;
        var count = !_configuration.GetCVar(UiMotionSettings.Enabled) || !_configuration.GetCVar(UiMotionSettings.SpeechTyping)
            ? _reveal.Count : _reveal.VisibleCount(_elapsed,
                UiMotionSettings.Read(_configuration, UiMotionSettings.SpeechCharactersPerSecond, 10, 120) * speed,
                pause, UiMotionSettings.Read(_configuration, UiMotionSettings.SpeechMaxSeconds, 0.1f, 2.5f));
        if (count == _visible)
            return;
        _visible = count;
        _speech.SetMessage(_reveal.Reveal(count));
    }
}
// Arcane-End
