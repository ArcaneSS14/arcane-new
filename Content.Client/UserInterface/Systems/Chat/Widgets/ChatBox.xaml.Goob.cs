using System.Linq;
using Content.Shared.Chat;

namespace Content.Client.UserInterface.Systems.Chat.Widgets;

public partial class ChatBox
{
    private void UpdateCoalescence(bool value)
    {
        _coalescence = value;
        Repopulate();

        foreach (var child in Contents.Children.ToArray())
        {
            if (child.Name != "_v_scroll")
            {
                Contents.RemoveChild(child);
            }
        }
    }

        public void Repopulate()
        {
            Contents.Clear();
            ResetCoalescing(); // Arcane

            // Goobstation start
            foreach (var child in Contents.Children.ToArray())
            {
                if (child.Name != "_v_scroll")
                {
                    Contents.RemoveChild(child);
                }
            }
            // Goobstation end

            foreach (var message in _controller.History)
            {
                OnMessageAdded(message.Item2);
            }
        }

        private void OnChannelFilter(ChatChannel channel, bool active)
        {
            Contents.Clear();
            ResetCoalescing(); // Arcane

            // Goobstation start
            foreach (var child in Contents.Children.ToArray())
            {
                if (child.Name != "_v_scroll")
                {
                    Contents.RemoveChild(child);
                }
            }
            // Goobstation end

            foreach (var message in _controller.History)
            {
                OnMessageAdded(message.Item2);
            }

            if (active)
            {
                _controller.ClearUnfilteredUnreads(channel);
            }
        }

    // Arcane-Start
    /// <summary>
    /// The cleared contents no longer hold the last line, so the next message must not coalesce into it.
    /// </summary>
    private void ResetCoalescing()
    {
        _lastLine = null;
        _lastLineRepeatCount = 0;
    }
    // Arcane-End
}
