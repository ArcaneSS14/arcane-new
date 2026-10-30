// Arcane-Start
using System.Text;
using Content.Arcane.Common.CCVar;
using Content.Shared.Radio;
using Robust.Shared.Configuration;

namespace Content.Client.Radio;

/// <summary>
/// Resolves radio channel colors from the player's archived channel overrides.
/// </summary>
public sealed partial class RadioChannelColorSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _configuration = default!;

    private readonly Dictionary<string, Color> _overrides = new();
    private bool _useCustomColors;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_configuration, ArcaneCVars.UseCustomRadioChannelColors, value => _useCustomColors = value, true);
        Subs.CVar(_configuration, ArcaneCVars.RadioChannelColors, OnColorsChanged, true);
    }

    public Color GetColor(string channelId)
    {
        if (_useCustomColors && _overrides.TryGetValue(channelId, out var color))
            return color;

        return ProtoMan.TryIndex<RadioChannelPrototype>(channelId, out var channel)
            ? channel.Color
            : Color.White;
    }

    public static Dictionary<string, Color> Parse(string value)
    {
        var result = new Dictionary<string, Color>();
        foreach (var entry in value.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var split = entry.Split('=', 2);
            var channelId = split.Length == 2 ? split[0].Trim() : string.Empty;
            if (channelId.Length == 0 || split.Length != 2 || !Color.TryFromHex(split[1].Trim(), out var color))
                continue;

            result[channelId] = color;
        }

        return result;
    }

    public static string Serialize(IEnumerable<KeyValuePair<string, Color>> colors)
    {
        var builder = new StringBuilder();
        foreach (var (channelId, color) in colors)
        {
            if (string.IsNullOrWhiteSpace(channelId) || channelId.Contains(';') || channelId.Contains('='))
                continue;

            if (builder.Length > 0)
                builder.Append(';');

            builder.Append(channelId).Append('=').Append(color.ToHex());
        }

        return builder.ToString();
    }

    private void OnColorsChanged(string value)
    {
        _overrides.Clear();
        foreach (var (channelId, color) in Parse(value))
        {
            if (ProtoMan.HasIndex<RadioChannelPrototype>(channelId))
                _overrides[channelId] = color;
        }
    }
}
// Arcane-End
