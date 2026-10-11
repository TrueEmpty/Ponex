using UnityEngine;

/// <summary>
/// Unity 6 refuses duration edits while a ParticleSystem is playing.
/// AddComponent starts playOnAwake immediately, so Stop() in the same frame is not enough.
/// </summary>
public static class ParticleEdit
{
    public static ParticleSystem AddStopped(GameObject go)
    {
        bool wasActive = go.activeSelf;
        go.SetActive(false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        go.SetActive(wasActive);
        return ps;
    }

    public static void PauseForEdit(ParticleSystem ps)
    {
        if (ps == null)
            return;
        // ParticleSystem is not a Behaviour — it has no .enabled.
        ps.gameObject.SetActive(false);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
    }

    public static void Resume(ParticleSystem ps, bool play)
    {
        if (ps == null)
            return;
        ps.gameObject.SetActive(true);
        if (play)
            ps.Play(true);
    }
}
