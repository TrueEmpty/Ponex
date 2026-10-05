using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Yuotay bat / mit one-shots. Clips live under Resources/Sounds/Yuotay:
/// hit_*, bunt_*, whoosh_*, catch_*, throw_*.
/// </summary>
public static class YuotaySfx
{
    const string ResourceFolder = "Sounds/Yuotay";

    static AudioClip[] hitClips;
    static AudioClip[] buntClips;
    static AudioClip[] whooshClips;
    static AudioClip[] catchClips;
    static AudioClip[] throwClips;
    static bool loaded;

    /// <summary>Bat contact crack on a real swing.</summary>
    public static void PlayBatHit(float volume = 1f)
    {
        EnsureLoaded();
        Play(hitClips, volume, 0.07f);
    }

    /// <summary>Soft deadened bunt tap.</summary>
    public static void PlayBunt(float volume = 0.85f)
    {
        EnsureLoaded();
        Play(buntClips, volume, 0.05f);
    }

    /// <summary>Air whoosh while the bat is swinging.</summary>
    public static void PlaySwingWhoosh(float volume = 0.8f)
    {
        EnsureLoaded();
        Play(whooshClips, volume, 0.06f);
    }

    public static void PlayCatch(float volume = 1f)
    {
        EnsureLoaded();
        Play(catchClips, volume, 0.06f);
    }

    public static void PlayThrow(float volume = 0.95f)
    {
        EnsureLoaded();
        Play(throwClips, volume, 0.07f);
    }

    static void EnsureLoaded()
    {
        if (loaded)
            return;

        AudioClip[] all = Resources.LoadAll<AudioClip>(ResourceFolder);
        hitClips = Filter(all, "hit_");
        buntClips = Filter(all, "bunt_");
        whooshClips = Filter(all, "whoosh_");
        catchClips = Filter(all, "catch_");
        throwClips = Filter(all, "throw_");
        loaded = true;
    }

    static AudioClip[] Filter(AudioClip[] all, string prefix)
    {
        if (all == null || all.Length == 0)
            return System.Array.Empty<AudioClip>();

        List<AudioClip> list = new List<AudioClip>();
        for (int i = 0; i < all.Length; i++)
        {
            AudioClip c = all[i];
            if (c == null || string.IsNullOrEmpty(c.name))
                continue;
            if (c.name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                list.Add(c);
        }
        return list.ToArray();
    }

    static void Play(AudioClip[] clips, float volume, float pitchJitter)
    {
        if (clips == null || clips.Length == 0)
            return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null)
            return;

        GameObject go = new GameObject("YuotaySfx");
        AudioSource src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.volume = Mathf.Clamp01(volume);
        src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        src.clip = clip;
        src.Play();
        Object.Destroy(go, (clip.length / Mathf.Max(0.01f, src.pitch)) + 0.05f);
    }
}
