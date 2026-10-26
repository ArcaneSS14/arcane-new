using System.Numerics;
using Content.Client.Gameplay;
using Content.Shared.Popups;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using System.Text; // Arcane

namespace Content.Client.Popups;

/// <summary>
/// Handles screens-space popups. World popups are handled via PopupOverlay.
/// </summary>
public sealed class PopupUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [UISystemDependency] private readonly PopupSystem? _popup = default!;

    private Font _smallFont = default!;
    private Font _mediumFont = default!;
    private Font _largeFont = default!;

    private PopupRootControl? _popupControl;

    // Arcane-Start
    private readonly Dictionary<PopupSystem.PopupLabel, PopupTextLayout> _popupLayouts = [];

    private sealed class PopupTextLayout(
        Font font,
        string sourceText,
        float sourceScale,
        UIBox2 bounds,
        string text,
        float scale,
        Vector2 dimensions)
    {
        public bool Matches(Font otherFont, string otherText, float otherScale, UIBox2 otherBounds)
        {
            return ReferenceEquals(font, otherFont)
                   && sourceText == otherText
                   && sourceScale == otherScale
                   && bounds.Equals(otherBounds);
        }

        public (string Text, float Scale, Vector2 Dimensions) Result => (text, scale, dimensions);
    }
    // Arcane-End

    public override void Initialize()
    {
        base.Initialize();
        var cache = IoCManager.Resolve<IResourceCache>();

        _smallFont = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Italic.ttf"), 10);
        _mediumFont = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Italic.ttf"), 12);
        _largeFont = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-BoldItalic.ttf"), 14);
    }

    public void OnStateEntered(GameplayState state)
    {
        _popupControl = new PopupRootControl(_popup, this);

        UIManager.RootControl.AddChild(_popupControl);
    }

    public void OnStateExited(GameplayState state)
    {
        if (_popupControl == null)
            return;

        UIManager.RootControl.RemoveChild(_popupControl);
        _popupControl = null;
        _popupLayouts.Clear(); // Arcane
    }

    public void DrawPopup(PopupSystem.PopupLabel popup, DrawingHandleScreen handle, Vector2 position, float scale, UIBox2 bounds) // Arcane-Edit: without bounds > with bounds
    {
        var lifetime = PopupSystem.GetPopupLifetime(popup);

        // Keep alpha at 1 until TotalTime passes half its lifetime, then gradually decrease to 0.
        var alpha = MathF.Min(1f, 1f - MathF.Max(0f, popup.TotalTime - lifetime / 2) * 2 / lifetime);

        var updatedPosition = position - new Vector2(0f, MathF.Min(8f, 12f * (popup.TotalTime * popup.TotalTime + popup.TotalTime)));
        var font = _smallFont;
        var color = Color.White.WithAlpha(alpha);

        switch (popup.Type)
        {
            case PopupType.SmallCaution:
                color = Color.Red;
                break;
            case PopupType.Medium:
                font = _mediumFont;
                color = Color.LightGray;
                break;
            case PopupType.MediumCaution:
                font = _mediumFont;
                color = Color.Red;
                break;
            case PopupType.Large:
                font = _largeFont;
                color = Color.LightGray;
                break;
            case PopupType.LargeCaution:
                font = _largeFont;
                color = Color.Red;
                break;
        }

        // Arcane-Edit-Start
        var (text, textScale, dimensions) = GetPopupLayout(popup, handle, font, scale, bounds);
        // Arcane-Edit-End
        var drawPosition = updatedPosition - dimensions / 2f;
        // Arcane-Start
        drawPosition = ClampPopupPosition(drawPosition + popup.RandomOffset, dimensions, bounds);
        // Arcane-End
        var outline = TextOutline.Default with { Color = TextOutline.Default.Color.WithAlpha(alpha) };
        handle.DrawString(font, drawPosition, text, textScale, color.WithAlpha(alpha), outline); // Arcane-Edit: direct popup text > wrapped popup text
    }

    // Arcane-Start
    private (string Text, float Scale, Vector2 Dimensions) GetPopupLayout(
        PopupSystem.PopupLabel popup,
        DrawingHandleScreen handle,
        Font font,
        float scale,
        UIBox2 bounds)
    {
        if (_popupLayouts.TryGetValue(popup, out var cached) && cached.Matches(font, popup.Text, scale, bounds))
            return cached.Result;

        var (text, layoutScale, dimensions) = LayoutPopupText(handle, font, popup.Text, scale, bounds);
        _popupLayouts[popup] = new PopupTextLayout(font, popup.Text, scale, bounds, text, layoutScale, dimensions);
        PrunePopupLayouts();
        return (text, layoutScale, dimensions);
    }

    private void PrunePopupLayouts()
    {
        if (_popup == null || _popupLayouts.Count <= _popup.WorldLabels.Count + _popup.CursorLabels.Count)
            return;

        var stale = new List<PopupSystem.PopupLabel>();
        foreach (var label in _popupLayouts.Keys)
        {
            if (IsActivePopup(label))
                continue;

            stale.Add(label);
        }

        foreach (var label in stale)
        {
            _popupLayouts.Remove(label);
        }
    }

    private bool IsActivePopup(PopupSystem.PopupLabel label)
    {
        foreach (var active in _popup!.WorldLabels)
        {
            if (ReferenceEquals(active, label))
                return true;
        }

        foreach (var active in _popup.CursorLabels)
        {
            if (ReferenceEquals(active, label))
                return true;
        }

        return false;
    }

    private static (string Text, float Scale, Vector2 Dimensions) LayoutPopupText(
        DrawingHandleScreen handle,
        Font font,
        string text,
        float scale,
        UIBox2 bounds)
    {
        var availableWidth = MathF.Max(1f, bounds.Right - bounds.Left);
        var availableHeight = MathF.Max(1f, bounds.Bottom - bounds.Top);
        var wrapped = text;
        var dimensions = handle.GetDimensions(font, wrapped, scale);

        if (dimensions.X <= availableWidth && dimensions.Y <= availableHeight)
            return (wrapped, scale, dimensions);

        for (var i = 0; i < 8; i++)
        {
            wrapped = WrapPopupText(font, text, scale, availableWidth);
            dimensions = handle.GetDimensions(font, wrapped, scale);

            if (dimensions.X <= availableWidth && dimensions.Y <= availableHeight)
                return (wrapped, scale, dimensions);

            var widthScale = dimensions.X > 0f ? availableWidth / dimensions.X : 1f;
            var heightScale = dimensions.Y > 0f ? availableHeight / dimensions.Y : 1f;
            scale *= MathF.Min(widthScale, heightScale);
        }

        wrapped = WrapPopupText(font, text, scale, availableWidth);
        dimensions = handle.GetDimensions(font, wrapped, scale);
        return (wrapped, scale, dimensions);
    }

    private static string WrapPopupText(Font font, string text, float scale, float availableWidth)
    {
        var result = new StringBuilder(text.Length);
        var line = new List<(Rune Character, float Advance)>();
        var lineWidth = 0f;

        foreach (var rune in text.EnumerateRunes())
        {
            if (rune.Value == '\n')
            {
                AppendLine(line, result);
                result.Append('\n');
                line.Clear();
                lineWidth = 0f;
                continue;
            }

            var advance = font.TryGetCharMetrics(rune, scale, out var metrics) ? metrics.Advance : 0f;
            line.Add((rune, advance));
            lineWidth += advance;

            if (lineWidth <= availableWidth || line.Count == 1)
                continue;

            var breakIndex = line.FindLastIndex(entry => Rune.IsWhiteSpace(entry.Character));
            var splitIndex = breakIndex >= 0 ? breakIndex + 1 : line.Count - 1;
            AppendLine(line, result, splitIndex);
            result.Append('\n');

            line = line.GetRange(splitIndex, line.Count - splitIndex);
            lineWidth = 0f;
            foreach (var entry in line)
            {
                lineWidth += entry.Advance;
            }
        }

        AppendLine(line, result);
        return result.ToString();
    }

    private static void AppendLine(List<(Rune Character, float Advance)> line, StringBuilder result, int count = int.MaxValue)
    {
        for (var i = 0; i < Math.Min(count, line.Count); i++)
        {
            result.Append(line[i].Character.ToString());
        }
    }

    private static Vector2 ClampPopupPosition(Vector2 position, Vector2 dimensions, UIBox2 bounds)
    {
        var maximum = new Vector2(
            MathF.Max(bounds.Left, bounds.Right - dimensions.X),
            MathF.Max(bounds.Top, bounds.Bottom - dimensions.Y));
        return Vector2.Clamp(position, bounds.TopLeft, maximum);
    }
    // Arcane-End

    /// <summary>
    /// Handles drawing all screen popups.
    /// </summary>
    private sealed class PopupRootControl : Control
    {
        private readonly PopupSystem? _popup;
        private readonly PopupUIController _controller;

        public PopupRootControl(PopupSystem? system, PopupUIController controller)
        {
            _popup = system;
            _controller = controller;
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            if (_popup == null)
                return;

            // Different window
            var windowId = UserInterfaceManager.RootControl.Window.Id;

            foreach (var popup in _popup.CursorLabels)
            {
                if (popup.InitialPos.Window != windowId)
                    continue;

                _controller.DrawPopup(popup, handle, popup.InitialPos.Position, UIScale, PixelSizeBox); // Arcane-Edit: without bounds > with bounds
            }
        }
    }
}
