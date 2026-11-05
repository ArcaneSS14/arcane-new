using System.Linq;
using Content.Arcane.Client.JoinQueue;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class QueueLeaderboardTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = false, Dirty = true };

    [Test]
    public async Task ReplacementQueueListsImmediatelyReplaceDisplayedPlayers()
    {
        await Client.WaitAssertion(() =>
        {
            var board = new QueueTimeLeaderboardControl();
            var root = Client.ResolveDependency<IUserInterfaceManager>().StateRoot;
            root.AddChild(board);
            try
            {
                board.UpdateQueuePlayers(new[] {"Alice"}, new[] {100f}, "");
                Assert.That(Labels(board), Does.Contain("1. Alice"));
                board.UpdateQueuePlayers(new[] {"Bob"}, new[] {50f}, "");
                Assert.That(Labels(board), Does.Contain("1. Bob"));
                Assert.That(Labels(board), Does.Not.Contain("1. Alice"), "A departed player must disappear before the next timer tick.");
                board.UpdateQueuePlayers(Array.Empty<string>(), Array.Empty<float>(), "");
                Assert.That(Labels(board), Does.Not.Contain("1. Bob"));
            }
            finally
            {
                root.RemoveChild(board);
            }
        });
    }

    private static IEnumerable<string?> Labels(Control control)
    {
        if (control is Label label)
            yield return label.Text;

        foreach (var text in control.Children.SelectMany(Labels))
            yield return text;
    }
}
