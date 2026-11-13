#nullable enable // Arcane
using System.Linq;
using Content.Arcane.Client.JoinQueue;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Localization;

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
                board.UpdateQueuePlayers(new[] {"Alice"}, new[] {100f}, -1);
                Assert.That(Labels(board), Does.Contain("1. Alice"));
                board.UpdateQueuePlayers(new[] {"Bob"}, new[] {50f}, -1);
                Assert.That(Labels(board), Does.Contain("1. Bob"));
                Assert.That(Labels(board), Does.Not.Contain("1. Alice"), "A departed player must disappear before the next timer tick.");
                board.UpdateQueuePlayers(Array.Empty<string>(), Array.Empty<float>(), -1);
                Assert.That(Labels(board), Does.Not.Contain("1. Bob"));
            }
            finally
            {
                root.RemoveChild(board);
            }
        });
    }

    [Test]
    public async Task ViewerMarkerFollowsTheOriginalRowAfterSortingAndFiltering()
    {
        await Client.WaitAssertion(() =>
        {
            using var board = new QueueTimeLeaderboardControl();
            board.UpdateQueuePlayers(
                new[] { "Invalid", "Same name", "Same name", "Longest" },
                new[] { float.NaN, 100f, 200f, 300f },
                1);

            var you = Loc.GetString("queue-leaderboard-you", ("name", "Same name"));
            Assert.That(Labels(board), Does.Contain("1. Longest"));
            Assert.That(Labels(board), Does.Contain("2. Same name"));
            Assert.That(Labels(board), Does.Contain($"3. {you}"));
            Assert.That(Labels(board).Count(text => text?.Contains(you) == true), Is.EqualTo(1));

            board.UpdateQueuePlayers(new[] { "Same name" }, new[] { 200f }, -1);
            Assert.That(Labels(board), Does.Contain("1. Same name"));
            Assert.That(Labels(board).Any(text => text?.Contains(you) == true), Is.False);
        });
    }

    [Test]
    public async Task InvalidTimesCannotDisplaceValidRankedRows()
    {
        await Client.WaitAssertion(() =>
        {
            using var board = new QueueTimeLeaderboardControl();
            board.UpdateQueuePlayers(
                new[] { "NaN", "Positive infinity", "Negative infinity", "Overflow", "Negative", "Hour", "Minute", "Second", "Zero" },
                new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, 2147483648f, -1f, 3600f, 60f, 59f, 0f },
                -1);

            Assert.That(Labels(board), Does.Contain("1. Hour"));
            Assert.That(Labels(board), Does.Contain("2. Minute"));
            Assert.That(Labels(board), Does.Contain("3. Second"));
            Assert.That(Labels(board), Does.Contain("4. Zero"));
            Assert.That(Labels(board), Does.Contain(Loc.GetString("queue-leaderboard-time-hours", ("hours", 1), ("minutes", 0))));
            Assert.That(Labels(board), Does.Contain(Loc.GetString("queue-leaderboard-time-minutes", ("minutes", 1), ("seconds", 0))));
            Assert.That(Labels(board), Does.Contain(Loc.GetString("queue-leaderboard-time-seconds", ("seconds", 59))));
            Assert.That(Labels(board), Does.Contain(Loc.GetString("queue-leaderboard-time-seconds", ("seconds", 0))));
            Assert.That(Labels(board).Count(text => text == Loc.GetString("queue-leaderboard-empty")), Is.EqualTo(1));

            board.UpdateQueuePlayers(new[] { "Valid edge" }, new[] { 2147483520f }, -1);
            Assert.That(Labels(board), Does.Contain("1. Valid edge"));
            Assert.That(Labels(board), Does.Contain(Loc.GetString("queue-leaderboard-time-hours", ("hours", 596523), ("minutes", 12))));
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
