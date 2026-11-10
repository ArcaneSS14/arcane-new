using System.Linq;
using Content.Arcane.Client.JoinQueue.SpaceInvaders;
using Content.Arcane.Client.JoinQueue.Gyruss;
using Content.Arcane.Client.JoinQueue.GoGoShitcurity;
using Content.Arcane.Server.JoinQueue;
using Content.Arcane.Shared.JoinQueue;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class QueueMiniGameLogicTest
{
    [Test]
    public void SweepsChooseTheFirstEnemyOnThePathInEveryGame()
    {
        var gyruss = new GyrussGame();
        gyruss.Reset();
        var near = new GyrussGame.Enemy {Angle = gyruss.PlayerAngle, Radius = 135f, RadialSpeed = 23f};
        var far = new GyrussGame.Enemy {Angle = gyruss.PlayerAngle, Radius = 112f, RadialSpeed = 23f};
        gyruss.Enemies.AddRange([near, far]);
        gyruss.Bullets.Add(new GyrussGame.Bullet(gyruss.PlayerAngle, 148f, -280f, true));
        gyruss.Update(0.1f, false, false, false);
        Assert.That(gyruss.Enemies, Is.EqualTo(new[] {far}));

        var goGo = new GoGoShitcurityGame();
        goGo.Reset();
        var nearGoGo = new GoGoShitcurityGame.Enemy {X = 110f, Y = 100f};
        var farGoGo = new GoGoShitcurityGame.Enemy {X = 140f, Y = 100f};
        goGo.Enemies.AddRange([nearGoGo, farGoGo]);
        goGo.Bullets.Add(new GoGoShitcurityGame.Bullet {X = 100f, Y = 110f, VX = 800f, IsPlayer = true});
        goGo.Update(0.1f, false, false, false, false, false);
        Assert.That(goGo.Enemies, Is.EqualTo(new[] {farGoGo}));

        var invaders = new SpaceInvadersGame();
        invaders.Reset();
        invaders.Crawlers.Clear();
        var nearCrawler = new SpaceInvadersGame.Crawler {X = 100f, Y = 130f};
        var farCrawler = new SpaceInvadersGame.Crawler {X = 100f, Y = 100f};
        invaders.Crawlers.AddRange([nearCrawler, farCrawler]);
        invaders.Bullets.Add(new SpaceInvadersGame.Bullet(101f, 140f, 0f, -330f, true));
        invaders.Update(0.1f, false, false, false);
        Assert.That(invaders.Crawlers, Is.EqualTo(new[] {farCrawler}));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void GoGoSweepStopsAtTheProjectileLifetimeBoundary(bool hitBeforeBoundary)
    {
        var game = new GoGoShitcurityGame();
        game.Reset();
        game.Enemies.Add(new GoGoShitcurityGame.Enemy {X = hitBeforeBoundary ? 550f : 590f, Y = 100f});
        game.Bullets.Add(new GoGoShitcurityGame.Bullet {X = 566f, Y = 110f, VX = 380f, IsPlayer = true});
        game.Update(0.1f, false, false, false, false, false);
        Assert.That(game.Score, Is.EqualTo(hitBeforeBoundary ? 10 : 0));
        Assert.That(game.Bullets, Is.Empty);
    }

    [Test]
    public void GyrussResetAllowsAnImmediateFirstShot()
    {
        var game = new GyrussGame();
        var shots = 0;
        game.OnShoot = () => shots++;
        game.Reset();
        game.Update(0f, false, false, true);
        game.Reset();
        game.Update(0f, false, false, true);
        Assert.That(shots, Is.EqualTo(2));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void GyrussHitsEnemiesCrossingTheBulletDuringASlowFrame(bool angularMotion)
    {
        var game = new GyrussGame();
        game.Reset();
        game.Enemies.Add(new GyrussGame.Enemy
        {
            Angle = game.PlayerAngle + (angularMotion ? 0.2f : 0f),
            AngularSpeed = angularMotion ? -4f : 0f,
            Radius = 100f,
            RadialSpeed = angularMotion ? 0f : 150f,
        });
        game.Bullets.Add(new GyrussGame.Bullet(game.PlayerAngle, angularMotion ? 100f : 128f,
            angularMotion ? 0f : -280f, true));
        game.Update(0.1f, false, false, false);
        Assert.That(game.Score, Is.GreaterThan(0));
        Assert.That(game.Enemies, Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void GoGoBulletsHitAlongTheirWholePath(bool playerBullet)
    {
        var game = new GoGoShitcurityGame();
        game.Reset();
        if (playerBullet)
            game.Enemies.Add(new GoGoShitcurityGame.Enemy {X = 120f, Y = 100f});
        game.Bullets.Add(new GoGoShitcurityGame.Bullet
        {
            X = 100f,
            Y = playerBullet ? 110f : game.PlayerY + 5f,
            VX = playerBullet ? 800f : -1000f,
            IsPlayer = playerBullet,
        });
        game.Update(0.1f, false, false, false, false, false);
        Assert.That(playerBullet ? game.Score : game.Lives, Is.EqualTo(playerBullet ? 10 : 2));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void InvadersBulletsHitAlongTheirWholePath(bool playerBullet)
    {
        var game = new SpaceInvadersGame();
        game.Reset();
        game.Crawlers.Clear();
        if (playerBullet)
            game.Crawlers.Add(new SpaceInvadersGame.Crawler {X = 100f, Y = 140f});
        game.Bullets.Add(new SpaceInvadersGame.Bullet(playerBullet ? 101f : game.PlayerX + 5f,
            playerBullet ? 180f : game.PlayerY - 20f, 0f, playerBullet ? -1000f : 1000f, playerBullet));
        game.Update(0.1f, false, false, false);
        Assert.That(playerBullet ? game.Score : game.Lives, Is.EqualTo(playerBullet ? 12 : 2));
    }

    [Test]
    public void InvadersMovementPreservesAccumulatedTimeAcrossFrameRates()
    {
        var slow = new SpaceInvadersGame();
        var fast = new SpaceInvadersGame();
        foreach (var game in new[] {slow, fast})
        {
            game.Reset();
            game.Crawlers.Clear();
            game.Crawlers.Add(new SpaceInvadersGame.Crawler {X = 100f, Y = 80f});
        }
        for (var i = 0; i < 10; i++)
            slow.Update(0.1f, false, false, false);
        for (var i = 0; i < 40; i++)
            fast.Update(0.025f, false, false, false);
        Assert.That(slow.Crawlers[0].X, Is.EqualTo(fast.Crawlers[0].X).Within(0.01f));
    }

    [Test]
    public void InvadersFullVictoryScoreFitsTheServerLeaderboard()
    {
        var game = new SpaceInvadersGame();
        game.Reset();
        for (var wave = 1; wave <= 7; wave++)
        {
            Assert.That(game.Wave, Is.EqualTo(wave));
            Assert.That(game.Crawlers.Select(crawler => (crawler.X, crawler.Y)).Distinct().Count(),
                Is.EqualTo(game.Crawlers.Count), "A formation must not stack duplicate enemies in the same cell.");
            var health = game.CoreHealth;
            for (var hit = 0; hit < health; hit++)
            {
                game.Bullets.Add(new SpaceInvadersGame.Bullet(game.CoreX + 1f, game.CoreY + 1f, 0f, 0f, true));
                game.Update(0f, false, false, false);
            }
            foreach (var crawler in game.Crawlers.ToArray())
            {
                game.Bullets.Add(new SpaceInvadersGame.Bullet(crawler.X + 1f, crawler.Y + 1f, 0f, 0f, true));
                game.Update(0f, false, false, false);
            }
        }

        Assert.That(game.Victory, Is.True);
        Assert.That(game.Score, Is.EqualTo(6480));
        Assert.That(JoinQueueSystem.GetMaxScore(QueueMiniGameKind.SpaceInvaders), Is.GreaterThanOrEqualTo(game.Score),
            "Completing the actual seven waves must not lose points when submitted to the server.");
    }

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
        if (!enemyDefeat)
            game.Bullets.Add(new GyrussGame.Bullet(game.PlayerAngle, 50f, 0f, true));
        game.Bullets.Add(new GyrussGame.Bullet(game.PlayerAngle, GyrussGame.PlayerRadius, 0f, false));
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
        if (!crawlerDefeat)
            game.Bullets.Add(new SpaceInvadersGame.Bullet(game.PlayerX + 1f, game.PlayerY + 1f, 0f, 0f, false));
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
