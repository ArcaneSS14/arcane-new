using Content.Arcane.Client.JoinQueue.SpaceInvaders;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class QueueMiniGameLogicTest
{
    [Test]
    public void DestroyedCoreDoesNotAbsorbBulletsOrAwardMorePoints()
    {
        var game = new SpaceInvadersGame();
        game.Reset();
        var health = game.CoreHealth;
        for (var i = 0; i < health; i++)
        {
            game.Bullets.Add(new SpaceInvadersGame.Bullet(game.CoreX + 1f, game.CoreY + 1f, 0f, 0f, true));
            game.Update(0f, false, false, false);
        }
        Assert.That(game.CoreHealth, Is.Zero);
        Assert.That(game.Score, Is.EqualTo(25 * health));
        Assert.That(game.Crawlers, Is.Not.Empty, "Live crawlers keep the current wave active after the core is destroyed.");

        var projectile = new SpaceInvadersGame.Bullet(game.CoreX + 1f, game.CoreY + 1f, 0f, 0f, true);
        game.Bullets.Add(projectile);
        game.Update(0f, false, false, false);
        Assert.That(game.CoreHealth, Is.Zero);
        Assert.That(game.Score, Is.EqualTo(25 * health));
        Assert.That(game.Bullets, Does.Contain(projectile));
    }
}
