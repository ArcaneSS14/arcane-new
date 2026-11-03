// Arcane-Start
using System.Globalization;
using System.Text;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Motion;

/// <summary>
/// Reveals common Unicode text clusters while retaining every character for layout.
/// Uses only the public formatted-message API; never slices markup or changes font metrics.
/// </summary>
public sealed class UiTextReveal
{
    private readonly FormattedMessage _message;
    private readonly int[] _starts;
    private readonly bool[] _punctuation;
    private readonly int _punctuationCount;

    public int Count => _starts.Length;

    public UiTextReveal(FormattedMessage message)
    {
        _message = new FormattedMessage(message);
        var text = new StringBuilder();
        foreach (var node in message)
        {
            if (node.IsPlainText)
                text.Append(node.Value.StringValue);
        }

        var plain = text.ToString();
        _starts = ParseGraphemeStarts(plain);
        _punctuation = new bool[Count];
        for (var i = 0; i < Count; i++)
        {
            _punctuation[i] = ".,!?;:…".Contains(plain[_starts[i]]);
            if (_punctuation[i] && i + 1 < Count)
                _punctuationCount++;
        }
    }

    // StringInfo is unavailable in the content sandbox. This deliberately bounded segmentation
    // covers combining marks, emoji joiners/modifiers/tags, flag pairs and CRLF using allowed Rune APIs.
    // It is not a complete UAX #29 implementation: decomposed Hangul and Indic consonant conjuncts
    // may reveal in multiple steps. Surrogate pairs are never split.
    private static int[] ParseGraphemeStarts(string text)
    {
        var starts = new List<int>();
        var offset = 0;
        var previous = -1;
        var regionalCount = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            var value = rune.Value;
            var category = Rune.GetUnicodeCategory(rune);
            var regional = value is >= 0x1F1E6 and <= 0x1F1FF;
            var extend = category is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark
                || value == 0x200D || previous == 0x200D
                || value is >= 0x1F3FB and <= 0x1F3FF or >= 0xE0020 and <= 0xE007F
                || previous == '\r' && value == '\n'
                || regional && regionalCount % 2 == 1;
            if (offset == 0 || !extend)
                starts.Add(offset);
            regionalCount = regional ? regionalCount + 1 : 0;
            offset += rune.Utf16SequenceLength;
            previous = value;
        }
        return starts.ToArray();
    }

    public int VisibleCount(float elapsed, float charactersPerSecond, float punctuationSeconds, float maxSeconds)
    {
        if (Count == 0 || elapsed >= maxSeconds)
            return Count;
        var interval = 1 / Math.Max(0.01f, charactersPerSecond);
        var total = Count * interval + _punctuationCount * punctuationSeconds;
        var budget = Math.Max(0, elapsed) * total / Math.Max(0.001f, Math.Min(total, maxSeconds));
        var time = 0f;
        for (var i = 0; i < Count; i++)
        {
            time += interval;
            if (time > budget)
                return i;
            if (_punctuation[i])
                time += punctuationSeconds;
        }
        return Count;
    }

    public FormattedMessage Reveal(int count)
    {
        count = Math.Clamp(count, 0, Count);
        if (count == Count)
            return new FormattedMessage(_message);

        var end = _starts[count];
        var position = 0;
        var result = new FormattedMessage();
        foreach (var node in _message)
        {
            if (!node.IsPlainText)
            {
                result.PushTag(node);
                continue;
            }

            var text = node.Value.StringValue ?? string.Empty;
            var visible = Math.Clamp(end - position, 0, text.Length);
            if (visible > 0)
                result.AddText(text[..visible]);
            if (visible < text.Length)
            {
                result.PushColor(Color.Transparent);
                result.AddText(text[visible..]);
                result.Pop();
            }
            position += text.Length;
        }
        return result;
    }
}
// Arcane-End
