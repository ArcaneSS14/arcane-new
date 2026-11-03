// Arcane-Start
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Motion;

/// <summary>Exercises the saved window, button, checkbox and speech preferences without changing configuration.</summary>
public sealed partial class UiMotionPreview : FancyWindow
{
    [Dependency] private IConfigurationManager _configuration = default!;
    private readonly RichTextLabel _speech;
    private readonly UiTextReveal _reveal;
    private float _elapsed;
    private int _visible = -1;
    private Popup? _popup;
    private FancyWindow? _sampleWindow;
    private sealed class PreviewSlot : SlotControl;

    public UiMotionPreview()
    {
        IoCManager.InjectDependencies(this);
        Title = Loc.GetString("ui-motion-preview-title");
        MinWidth = 360;
        var body = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
        ContentsContainer.AddChild(new ScrollContainer { MaxHeight = 600, Children = { body } });
        for (var i = 1; i <= 6; i++)
            body.AddChild(new Button { Text = Loc.GetString("ui-motion-preview-button", ("number", i)) });
        body.AddChild(new CheckBox { Text = Loc.GetString("ui-motion-preview-checkbox") });
        var windowButton = new Button { Text = Loc.GetString("ui-motion-preview-window") };
        windowButton.OnPressed += _ =>
        {
            _sampleWindow?.Close();
            _sampleWindow = new FancyWindow { Title = Loc.GetString("ui-motion-preview-window") };
            var close = new Button { Text = Loc.GetString("ui-motion-preview-close") };
            close.OnPressed += _ => _sampleWindow.Close();
            _sampleWindow.ContentsContainer.AddChild(close);
            _sampleWindow.OpenCentered();
        };
        body.AddChild(windowButton);
        var popupButton = new Button { Text = Loc.GetString("ui-motion-preview-popup"), ToolTip = Loc.GetString("ui-motion-preview-tooltip") };
        popupButton.OnPressed += _ =>
        {
            _popup?.Close();
            _popup = new Popup();
            _popup.AddChild(new Label { Text = Loc.GetString("ui-motion-preview-tooltip") });
            _popup.OpenAtMouse();
        };
        body.AddChild(popupButton);

        var menu = new UiMotionContainer();
        var menuGrid = new UiMotionGridContainer { Motion = menu, Columns = 1, MaxStaggeredChildren = 8 };
        menu.AddChild(menuGrid);
        for (var i = 1; i <= 4; i++)
            menuGrid.AddChild(new Button { Text = Loc.GetString("ui-motion-preview-button", ("number", i)) });
        var replayMenu = new Button { Text = Loc.GetString("ui-motion-preview-menu") };
        replayMenu.OnPressed += _ => { menu.UseMenuProfile(); menu.Show(); };
        body.AddChild(replayMenu);
        body.AddChild(menu);

        var slot = new PreviewSlot();
        var slotRow = new BoxContainer();
        slotRow.AddChild(slot);
        var toggleSlot = new Button { Text = Loc.GetString("ui-motion-preview-slot") };
        var occupied = false;
        toggleSlot.OnPressed += _ =>
        {
            occupied = !occupied;
            slot.SetPrototype(occupied ? "Pen" : null, false);
            slot.Highlight = occupied;
        };
        slotRow.AddChild(toggleSlot);
        body.AddChild(slotRow);

        var alert = new UiMotionContainer { EnabledSetting = UiMotionSettings.Alerts, ClosingSetting = UiMotionSettings.Alerts };
        var alertSurface = new Control { MinHeight = 24 };
        alertSurface.AddChild(new Label { Text = Loc.GetString("ui-motion-preview-alert") });
        var alertFeedback = new UiMotionFeedback();
        alertSurface.AddChild(alertFeedback);
        alert.AddChild(alertSurface);
        body.AddChild(alert);
        var changeAlert = new Button { Text = Loc.GetString("ui-motion-preview-alert-change") };
        changeAlert.OnPressed += _ =>
        {
            alert.Profile = new UiMotionProfile(TimeSpan.FromMilliseconds(UiMotionSettings.Read(_configuration, UiMotionSettings.AlertsEnterMs, 0, 1000)),
                TimeSpan.FromMilliseconds(UiMotionSettings.Read(_configuration, UiMotionSettings.AlertsExitMs, 0, 1000)), 0, false);
            if (alert.Visible)
            {
                alert.CreateExitVisual();
                alert.Visible = false;
            }
            else
            {
                alert.Visible = true;
                alert.Show();
            }
        };
        body.AddChild(changeAlert);
        var pulse = new Button { Text = Loc.GetString("ui-motion-preview-severity") };
        pulse.OnPressed += _ => alertFeedback.Play(UiMotionSettings.Alerts, UiMotionSettings.AlertsSeverityPulse, 150);
        body.AddChild(pulse);
        var progress = new UiMotionProgressBar { MaxValue = 1, Value = 0.25f, MinHeight = 16 };
        body.AddChild(progress);
        var changeProgress = new Button { Text = Loc.GetString("ui-motion-preview-progress") };
        changeProgress.OnPressed += _ => progress.Value = progress.Value < 0.5f ? 0.8f : 0.25f;
        body.AddChild(changeProgress);

        var rows = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
        var replayList = new Button { Text = Loc.GetString("ui-motion-preview-list") };
        replayList.OnPressed += _ =>
        {
            rows.RemoveAllChildren();
            for (var i = 1; i <= 4; i++)
            {
                var row = new UiMotionListRow();
                row.AddChild(new Label { Text = Loc.GetString("ui-motion-preview-button", ("number", i)) });
                rows.AddChild(row);
            }
        };
        body.AddChild(replayList);
        body.AddChild(rows);
        var tabs = new TabContainer { MinHeight = 80, TabsVisible = true };
        tabs.AddChild(new Label { Text = Loc.GetString("ui-motion-preview-tab-first") });
        tabs.SetTabTitle(0, Loc.GetString("ui-motion-preview-tab-first"));
        tabs.AddChild(new Label { Text = Loc.GetString("ui-motion-preview-tab-second") });
        tabs.SetTabTitle(1, Loc.GetString("ui-motion-preview-tab-second"));
        body.AddChild(tabs);
        var switchTab = new Button { Text = Loc.GetString("ui-motion-preview-tabs") };
        switchTab.OnPressed += _ => tabs.CurrentTab = (tabs.CurrentTab + 1) % tabs.ChildCount;
        body.AddChild(switchTab);
        _speech = new RichTextLabel { MaxWidth = 340 };
        body.AddChild(_speech);
        _reveal = new UiTextReveal(FormattedMessage.FromUnformatted(Loc.GetString("ui-motion-preview-speech")));
        _speech.SetMessage(_reveal.Reveal(0));
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        _elapsed += args.DeltaSeconds;
        var speed = UiMotionSettings.PlaybackSpeed(_configuration);
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

    public override void Close()
    {
        _popup?.Close();
        _sampleWindow?.Close();
        base.Close();
    }
}
// Arcane-End
