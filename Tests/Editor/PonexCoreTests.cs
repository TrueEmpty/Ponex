#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class PonexCoreTests
{
    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
        var liveBalls = new List<GameObject>();
        LiveBallRegistry.CopyLiveGameObjects(liveBalls);
        for (int i = liveBalls.Count - 1; i >= 0; i--)
            Object.DestroyImmediate(liveBalls[i]);

        if (MenuManager.instance != null)
            Object.DestroyImmediate(MenuManager.instance.gameObject);
    }

    [Test]
    public void LiveBallRegistryTracksEnabledBallInfo()
    {
        GameObject ball = new GameObject("Registry Test Ball");
        BallInfo info = ball.AddComponent<BallInfo>();

        Assert.AreEqual(1, LiveBallRegistry.Count);
        Assert.AreSame(info, LiveBallRegistry.GetAt(0));

        ball.SetActive(false);
        Assert.AreEqual(0, LiveBallRegistry.Count);
        Object.DestroyImmediate(ball);
    }

    [Test]
    public void MenuManagerReturnsUnderlyingNonOverlayMenu()
    {
        GameObject managerObject = new GameObject("Menu Manager Test");
        MenuManager manager = managerObject.AddComponent<MenuManager>();
        GameObject playing = new GameObject("Playing");
        GameObject pause = new GameObject("Pause");

        manager.menus.Add(new MenuClass("Playing", false, playing));
        manager.menus.Add(new MenuClass("Pause", true, pause));
        manager.openMenu.Add("Playing");
        manager.openMenu.Add("Pause");

        Assert.AreEqual("Pause", manager.GetOpenMenu().title);
        Assert.AreEqual("Playing", manager.GetOpenMenu(true).title);

        Object.DestroyImmediate(playing);
        Object.DestroyImmediate(pause);
    }

    [Test]
    public void TagListMatcherIsCaseInsensitiveAndTrimmed()
    {
        var tags = new List<string> { " Walls ", "Obstacle" };

        Assert.IsTrue(TagListMatcher.Contains(tags, "walls"));
        Assert.IsTrue(TagListMatcher.Contains(tags, " OBSTACLE "));
        Assert.IsFalse(TagListMatcher.Contains(tags, "Ball"));
    }

    [Test]
    public void ExplosionDealsOneDamageAndKeepsNariTailHealthInSync()
    {
        Player nari = new Player
        {
            name = "Nari",
            currentHealth = 5,
            maxHealth = 5
        };

        int lost = BallBlast.DamagePlayer(nari, null, 987654, 10);

        Assert.AreEqual(1, lost);
        Assert.AreEqual(4, nari.currentHealth);
        Assert.AreEqual(4, nari.maxHealth);
    }
}
#endif
