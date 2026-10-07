using System.Collections.Generic;
using UnityEngine;

/// <summary>Tracks active BallInfo objects so AI / Database avoid Find* every frame.</summary>
public static class LiveBallRegistry
{
    static readonly List<BallInfo> balls = new List<BallInfo>(32);

    public static int Count => balls.Count;

    public static void Register(BallInfo info)
    {
        if (info == null)
            return;
        if (!balls.Contains(info))
            balls.Add(info);
    }

    public static void Unregister(BallInfo info)
    {
        if (info == null)
            return;
        balls.Remove(info);
    }

    public static BallInfo GetAt(int index)
    {
        if (index < 0 || index >= balls.Count)
            return null;
        return balls[index];
    }

    /// <summary>Compact null/destroyed entries and copy live GameObjects into buffer.</summary>
    public static void CopyLiveGameObjects(List<GameObject> into)
    {
        if (into == null)
            return;
        into.Clear();
        for (int i = balls.Count - 1; i >= 0; i--)
        {
            BallInfo info = balls[i];
            if (info == null)
            {
                balls.RemoveAt(i);
                continue;
            }
            GameObject go = info.gameObject;
            if (go == null || !go.activeInHierarchy)
            {
                balls.RemoveAt(i);
                continue;
            }
            into.Add(go);
        }
    }

    public static void ForEachLive(System.Action<BallInfo> action)
    {
        if (action == null)
            return;
        for (int i = balls.Count - 1; i >= 0; i--)
        {
            BallInfo info = balls[i];
            if (info == null || info.gameObject == null)
            {
                balls.RemoveAt(i);
                continue;
            }
            action(info);
        }
    }
}
