// Arcane-Start
using Content.Client.Radio;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.RichText;

/// <summary>
/// Applies a radio channel's default or player-selected color to localized radio markup.
/// </summary>
public sealed class RadioChannelColorTag : IMarkupTagHandler
{
    [Dependency] private IEntitySystemManager _systems = default!;

    public string Name => "radiochannel";

    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        if (node.Attributes.TryGetValue("lang", out var language)
            && language.TryGetString(out var languageHex)
            && Color.TryFromHex(languageHex, out var languageColor))
        {
            context.Color.Push(languageColor);
            return;
        }

        if (!node.Value.TryGetString(out var channelId)
            || !_systems.TryGetEntitySystem<RadioChannelColorSystem>(out var colors))
        {
            context.Color.Push(ColorTag.DefaultColor);
            return;
        }

        context.Color.Push(colors.GetColor(channelId));
    }

    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Color.Pop();
    }
}
// Arcane-End
