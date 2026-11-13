using Content.Arcane.Server.JoinQueue;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests._Arcane;

[TestFixture]
public sealed class JoinQueueStateTest
{
    [Test]
    public void InterruptedAdmissionKeepsItsPlaceUntilConfirmedAndRejectsStaleCompletion()
    {
        var queue = new JoinQueueState<object>();
        var user = new NetUserId(Guid.NewGuid());
        var first = new object();
        var second = new object();
        var replacement = new object();
        queue.Connect(user, first, TimeSpan.Zero);
        queue.MarkReady(user, first);
        var secondId = new NetUserId(Guid.NewGuid());
        queue.Connect(secondId, second, TimeSpan.FromSeconds(1));
        queue.MarkReady(secondId, second);
        Assert.That(queue.TryReserve(out var reserved), Is.True);
        Assert.That(reserved.Session, Is.SameAs(first));
        Assert.That(queue.TryGet(user, out _), Is.True, "Reservation is not confirmed admission.");
        Assert.That(queue.Disconnect(user, first, TimeSpan.FromMinutes(2)), Is.True);
        var reconnected = queue.Connect(user, replacement, TimeSpan.FromSeconds(10));
        Assert.That(reconnected.WaitedSince, Is.EqualTo(TimeSpan.Zero));
        Assert.That(queue.Remove(user, first, out _), Is.False);
        queue.MarkReady(user, replacement);
        Assert.That(queue.TryReserve(out reserved), Is.True);
        Assert.That(reserved.Session, Is.SameAs(replacement));
        Assert.That(queue.TryReserve(out reserved), Is.True);
        Assert.That(reserved.Session, Is.SameAs(second), "A pending slot must not be reserved twice.");
        Assert.That(queue.TryReserve(out _), Is.False);
        Assert.That(queue.Remove(user, replacement, out _), Is.True);
        Assert.That(queue.TryGet(user, out _), Is.False);
    }

    [Test]
    public void ASlowerProfileLoadCannotBeOvertakenByALaterPlayer()
    {
        var queue = new JoinQueueState<object>();
        var first = new object();
        var second = new object();
        var firstId = new NetUserId(Guid.NewGuid());
        var secondId = new NetUserId(Guid.NewGuid());
        queue.Connect(firstId, first, TimeSpan.Zero);
        queue.Connect(secondId, second, TimeSpan.Zero);
        queue.MarkReady(secondId, second);
        Assert.That(queue.TryReserve(out _), Is.False);
        queue.MarkReady(firstId, first);
        Assert.That(queue.TryReserve(out var entry), Is.True);
        Assert.That(entry.Session, Is.SameAs(first));
        Assert.That(queue.TryReserve(out entry), Is.True);
        Assert.That(entry.Session, Is.SameAs(second));
    }

    [Test]
    public void ReconnectKeepsTheOriginalPositionAndIgnoresTheOldSessionsCallbacks()
    {
        var queue = new JoinQueueState<object>();
        var first = new object();
        var old = new object();
        var replacement = new object();
        var last = new object();
        var firstId = new NetUserId(Guid.NewGuid());
        var rejoiningId = new NetUserId(Guid.NewGuid());
        var lastId = new NetUserId(Guid.NewGuid());
        queue.Connect(firstId, first, TimeSpan.Zero);
        queue.MarkReady(firstId, first);
        queue.Connect(rejoiningId, old, TimeSpan.Zero);
        queue.Disconnect(rejoiningId, old, TimeSpan.FromMinutes(2));
        queue.Connect(lastId, last, TimeSpan.FromSeconds(10));
        queue.MarkReady(lastId, last);
        queue.Connect(rejoiningId, replacement, TimeSpan.FromSeconds(20));
        Assert.That(queue.MarkReady(rejoiningId, old), Is.False);
        Assert.That(queue.Disconnect(rejoiningId, old, TimeSpan.FromMinutes(2)), Is.False);
        Assert.That(queue.Remove(rejoiningId, old, out _), Is.False);
        queue.MarkReady(rejoiningId, replacement);
        Assert.That(queue.TryReserve(out var entry), Is.True);
        Assert.That(entry.Session, Is.SameAs(first));
        Assert.That(queue.TryReserve(out entry), Is.True);
        Assert.That(entry.Session, Is.SameAs(replacement));
        Assert.That(queue.TryReserve(out entry), Is.True);
        Assert.That(entry.Session, Is.SameAs(last));
    }

    [Test]
    public void OfflineReservationsDoNotBlockSlotsAndExpiredReservationsRejoinAtTheEnd()
    {
        var queue = new JoinQueueState<object>();
        var disconnected = new object();
        var second = new object();
        var third = new object();
        var returning = new object();
        var user = new NetUserId(Guid.NewGuid());
        var secondId = new NetUserId(Guid.NewGuid());
        var thirdId = new NetUserId(Guid.NewGuid());
        queue.Connect(user, disconnected, TimeSpan.Zero);
        queue.Disconnect(user, disconnected, TimeSpan.FromMinutes(2));
        queue.Connect(secondId, second, TimeSpan.FromSeconds(10));
        queue.MarkReady(secondId, second);
        Assert.That(queue.TryReserve(out var entry), Is.True);
        Assert.That(entry.Session, Is.SameAs(second));
        queue.Connect(thirdId, third, TimeSpan.FromSeconds(20));
        queue.MarkReady(thirdId, third);
        queue.Connect(user, returning, TimeSpan.FromMinutes(2));
        queue.MarkReady(user, returning);
        Assert.That(queue.TryReserve(out entry), Is.True);
        Assert.That(entry.Session, Is.SameAs(third));
        Assert.That(queue.TryReserve(out entry), Is.True);
        Assert.That(entry.Session, Is.SameAs(returning));
    }
}
