// Arcane-Start
using Content.Trauma.Common.Knowledge;
using Robust.Client.Graphics;

namespace Content.Trauma.Client.Knowledge.UI;

/// <summary>
/// One skill in <see cref="KnowledgeTab"/>, kept alive between updates so that changes are shown in place.
/// </summary>
public sealed class KnowledgeEntry : PanelContainer
{
    private const int MaxLevel = 100;

    private static readonly Color PanelColor = Color.FromHex("#1e1e26");
    private static readonly Color BarBackgroundColor = Color.FromHex("#0f0f14");
    private static readonly Color ExperienceColor = Color.FromHex("#4f8fd6");
    private static readonly Color BonusColor = Color.FromHex("#6fcf6f");
    private static readonly Color PenaltyColor = Color.FromHex("#e06c6c");
    private static readonly Color[] MasteryColors =
    [
        Color.FromHex("#9a9aa2"),
        Color.FromHex("#e6e6e6"),
        Color.FromHex("#6fcf6f"),
        Color.FromHex("#4f8fd6"),
        Color.FromHex("#e6b84f"),
    ];

    private readonly KnowledgeSystem _knowledge;
    private readonly SpriteSystem _sprite;
    private readonly StyleBoxFlat _stripeStyle = new();
    private readonly StyleBoxFlat _levelStyle = new();
    private readonly TextureRect _icon = new()
    {
        SetSize = new Vector2(32, 32),
        Stretch = TextureRect.StretchMode.KeepAspectCentered,
        VerticalAlignment = VAlignment.Top,
    };
    private readonly RichTextLabel _name = new() { HorizontalExpand = true };
    private readonly Label _mastery = new() { VerticalAlignment = VAlignment.Top };
    private readonly ProgressBar _levelBar;
    private readonly ProgressBar _experienceBar;
    private readonly Label _level = CreateValueLabel();
    private readonly Label _experience = CreateValueLabel();

    private KnowledgeInfo? _info;

    public KnowledgeEntry(KnowledgeSystem knowledge, SpriteSystem sprite, bool showProgress)
    {
        _knowledge = knowledge;
        _sprite = sprite;

        MouseFilter = MouseFilterMode.Pass; // for the tooltip to exist
        PanelOverride = new StyleBoxFlat { BackgroundColor = PanelColor };

        _levelBar = CreateBar(_levelStyle, MaxLevel);
        _experienceBar = CreateBar(new StyleBoxFlat { BackgroundColor = ExperienceColor }, 1);

        var title = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal };
        title.AddChild(_name);
        title.AddChild(_mastery);

        var details = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 4,
            Margin = new Thickness(0, 4),
        };
        details.AddChild(title);
        if (showProgress)
        {
            details.AddChild(CreateBarRow("knowledge-entry-level-label", _levelBar, _level));
            details.AddChild(CreateBarRow("knowledge-entry-experience-label", _experienceBar, _experience));
        }

        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 8,
        };
        row.AddChild(new PanelContainer { SetWidth = 4, PanelOverride = _stripeStyle });
        row.AddChild(_icon);
        row.AddChild(details);
        row.Margin = new Thickness(0, 0, 8, 0);

        AddChild(row);
    }

    public void SetInfo(KnowledgeInfo info)
    {
        if (_info == info)
            return;

        _info = info;

        var bonus = info.NetLevel - info.LearnedLevel;
        var signedBonus = bonus > 0 ? $"+{bonus}" : bonus.ToString();
        ToolTip = $"{info.Desc}\n{Loc.GetString("knowledge-entry-tooltip", ("learned", info.LearnedLevel), ("bonus", signedBonus))}";

        _stripeStyle.BackgroundColor = info.Color;
        _levelStyle.BackgroundColor = info.Color;
        _icon.Texture = info.Sprite is { } sprite ? _sprite.Frame0(sprite) : null;
        _name.Text = info.Name;
        _name.Modulate = info.Color;

        var mastery = _knowledge.GetMastery(info.NetLevel);
        _mastery.Text = _knowledge.GetMasteryString(mastery);
        _mastery.FontColorOverride = MasteryColors[Math.Clamp(mastery, 0, MasteryColors.Length - 1)];

        _levelBar.Value = info.NetLevel;
        _level.Text = bonus == 0
            ? Loc.GetString("knowledge-entry-level", ("level", info.NetLevel))
            : Loc.GetString("knowledge-entry-level-bonus", ("level", info.NetLevel), ("bonus", signedBonus));
        _level.FontColorOverride = bonus switch
        {
            > 0 => BonusColor,
            < 0 => PenaltyColor,
            _ => null,
        };

        var maxed = info.LearnedLevel >= MaxLevel;
        _experienceBar.MaxValue = Math.Max(info.ExpCost, 1);
        _experienceBar.Value = maxed ? _experienceBar.MaxValue : info.CurrentExp;
        _experience.Text = maxed
            ? Loc.GetString("knowledge-entry-experience-max")
            : Loc.GetString("knowledge-entry-experience", ("experience", info.CurrentExp), ("cost", info.ExpCost));
    }

    private static Label CreateValueLabel()
        => new()
        {
            MinWidth = 96,
            Align = Label.AlignMode.Right,
            StyleClasses = { "LabelSubText" },
        };

    private static ProgressBar CreateBar(StyleBox foreground, float max)
        => new()
        {
            MinValue = 0,
            MaxValue = max,
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
            SetHeight = 10,
            ForegroundStyleBoxOverride = foreground,
            BackgroundStyleBoxOverride = new StyleBoxFlat { BackgroundColor = BarBackgroundColor },
        };

    private static BoxContainer CreateBarRow(LocId label, ProgressBar bar, Label value)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
        };
        row.AddChild(new Label
        {
            Text = Loc.GetString(label),
            MinWidth = 36,
            StyleClasses = { "LabelSubText" },
        });
        row.AddChild(bar);
        row.AddChild(value);
        return row;
    }
}
// Arcane-End
