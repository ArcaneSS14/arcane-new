using Content.Arcane.Client.JoinQueue.SpaceInvaders;
using Content.Arcane.Client.JoinQueue.Gyruss;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class QueueMiniGameLogicTest
{
    [TestCase(true)]
    [TestCase(false)]
    public void GyrussStopsAllGameChangesAfterEnemyOrBulletDefeat(bool enemyDefeat)
    {
        var game = new GyrussGame();
        game.Reset();
        for (var i = 0; i < 8; i++)
        {
            foreach (var enemy in game.Enemies)
                enemy.RadialSpeed = 0f;
            game.Bullets.Clear();
            game.Update(1f, false, false, false);
        }
        game.Bullets.Clear();
        for (var i = 0; i < 2; i++)
        {
            game.Bullets.Add(new GyrussGame.Bullet(game.PlayerAngle, GyrussGame.PlayerRadius, 0f, false));
            game.Update(0f, false, false, false);
        }
        Assert.That(game.Lives, Is.EqualTo(1));
        game.Enemies.Clear();
        game.Enemies.Add(new GyrussGame.Enemy
        {
            Angle = game.PlayerAngle,
            Radius = enemyDefeat ? GyrussGame.PlayerRadius : 50f,
        });
        game.Bullets.Add(new GyrussGame.Bullet(game.PlayerAngle, GyrussGame.PlayerRadius, 0f, false));
        if (!enemyDefeat)
            game.Bullets.Add(new GyrussGame.Bullet(game.PlayerAngle, 50f, 0f, true));
        var endings = 0;
        var scoreAtEnd = -1;
        var livesAtEnd = -1;
        var waveAtEnd = -1;
        game.OnGameOver = () =>
        {
            endings++;
            scoreAtEnd = game.Score;
            livesAtEnd = game.Lives;
            waveAtEnd = game.Wave;
        };
        game.Update(0f, false, false, false);
        Assert.That(game.GameOver, Is.True);
        game.Update(1f, true, true, true);
        Assert.That(endings, Is.EqualTo(1));
        Assert.That(game.Score, Is.EqualTo(scoreAtEnd));
        Assert.That(game.Lives, Is.EqualTo(livesAtEnd));
        Assert.That(game.Wave, Is.EqualTo(waveAtEnd));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void InvadersStopsAllGameChangesAfterCrawlerOrBulletDefeat(bool crawlerDefeat)
    {
        var game = new SpaceInvadersGame();
        game.Reset();
        var health = game.CoreHealth;
        for (var i = 0; i < health; i++)
        {
            game.Bullets.Add(new SpaceInvadersGame.Bullet(game.CoreX + 1f, game.CoreY + 1f, 0f, 0f, true));
            game.Update(0f, false, false, false);
        }
        for (var i = 0; i < 2; i++)
        {
            game.Bullets.Add(new SpaceInvadersGame.Bullet(game.PlayerX + 1f, game.PlayerY + 1f, 0f, 0f, false));
            game.Update(0f, false, false, false);
        }
        Assert.That(game.Lives, Is.EqualTo(1));
        game.Crawlers.Clear();
        if (crawlerDefeat)
            game.Crawlers.Add(new SpaceInvadersGame.Crawler { X = 100f, Y = game.PlayerY - SpaceInvadersGame.CrawlerH - 4f });
        game.Bullets.Add(new SpaceInvadersGame.Bullet(game.PlayerX + 1f, game.PlayerY + 1f, 0f, 0f, false));
        var endings = 0;
        var scoreAtEnd = -1;
        var livesAtEnd = -1;
        var waveAtEnd = -1;
        game.OnGameOver = () =>
        {
            endings++;
            scoreAtEnd = game.Score;
            livesAtEnd = game.Lives;
            waveAtEnd = game.Wave;
        };
        game.Update(crawlerDefeat ? 1f : 0f, false, false, false);
        Assert.That(game.GameOver, Is.True);
        game.Update(1f, true, true, true);
        Assert.That(endings, Is.EqualTo(1));
        Assert.That(game.Score, Is.EqualTo(scoreAtEnd));
        Assert.That(game.Lives, Is.EqualTo(livesAtEnd));
        Assert.That(game.Wave, Is.EqualTo(waveAtEnd));
        Assert.That(game.Victory, Is.False);
    }

    [Test]
    public void InvadersLosesImmediatelyWhenAnEdgeDescentCrossesThePlayerLine()
    {
        var game = new SpaceInvadersGame();
        game.Reset();
        game.Crawlers.Clear();
        game.Crawlers.Add(new SpaceInvadersGame.Crawler
        {
            X = SpaceInvadersGame.GameW - SpaceInvadersGame.CrawlerW - 5f,
            Y = game.PlayerY - SpaceInvadersGame.CrawlerH - 5f,
        });
        game.Update(1f, false, false, false);
        Assert.That(game.GameOver, Is.True);
        Assert.That(game.Victory, Is.False);
    }

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
