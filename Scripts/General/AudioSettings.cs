using UnityEngine;

/// <summary>
/// Volume buses. Master multiplies every other bus.
/// </summary>
public class AudioSettings : MonoBehaviour
{
    public enum Bus
    {
        Music,
        Sfx,
        Narration,
        CharacterEffects,
        Announcer,
        Ambient
    }

    const string PrefPrefix = "Ponex.Audio.";

    public static AudioSettings instance;

    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float narrationVolume = 1f;
    [Range(0f, 1f)] public float characterEffectsVolume = 1f;
    [Range(0f, 1f)] public float announcerVolume = 1f;
    [Range(0f, 1f)] public float ambientVolume = 1f;

    AudioSource musicSource;
    AudioSource ambientSource;
    AudioSource oneShotSource;

    AudioClip selectedClip;
    AudioClip deselectedClip;
    AudioClip selectionClip;
    float nextSelectedAt;

    float savedMaster = -1f;
    float savedMusic = -1f;
    float savedSfx = -1f;
    float savedNarration = -1f;
    float savedCharacter = -1f;
    float savedAnnouncer = -1f;
    float savedAmbient = -1f;

    public static AudioSettings Ensure()
    {
        if (instance != null)
            return instance;

        AudioSettings existing = FindAnyObjectByType<AudioSettings>();
        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        GameObject go = new GameObject("Audio Settings");
        return go.AddComponent<AudioSettings>();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        LoadPrefs();
        EnsureSources();
        LoadUiClips();
    }

    void Update()
    {
        ApplyMusicVolume();
        ApplyAmbientVolume();
        SaveIfChanged();
    }

    public float Volume(Bus bus)
    {
        float channel = 1f;
        switch (bus)
        {
            case Bus.Music: channel = musicVolume; break;
            case Bus.Sfx: channel = sfxVolume; break;
            case Bus.Narration: channel = narrationVolume; break;
            case Bus.CharacterEffects: channel = characterEffectsVolume; break;
            case Bus.Announcer: channel = announcerVolume; break;
            case Bus.Ambient: channel = ambientVolume; break;
        }

        return Mathf.Clamp01(masterVolume * channel);
    }

    public static float Scale(Bus bus, float volume)
    {
        AudioSettings settings = Ensure();
        return Mathf.Clamp01(volume) * settings.Volume(bus);
    }

    public static void PlaySelected()
    {
        AudioSettings settings = Ensure();
        if (Time.unscaledTime < settings.nextSelectedAt)
            return;
        settings.nextSelectedAt = Time.unscaledTime + 0.08f;
        settings.PlayOneShot(settings.selectedClip, Bus.Sfx, 1f);
    }

    public static void PlayDeselected()
    {
        AudioSettings settings = Ensure();
        settings.PlayOneShot(settings.deselectedClip, Bus.Sfx, 1f);
    }

    public static void PlaySelection()
    {
        AudioSettings settings = Ensure();
        settings.PlayOneShot(settings.selectionClip, Bus.Sfx, 1f);
    }

    public static void PlaySfx(AudioClip clip, float volume = 1f)
    {
        Ensure().PlayOneShot(clip, Bus.Sfx, volume);
    }

    public static void PlayCharacterEffect(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        Ensure().PlayOneShot(clip, Bus.CharacterEffects, volume, pitch);
    }

    public static void PlayNarration(AudioClip clip, float volume = 1f)
    {
        Ensure().PlayOneShot(clip, Bus.Narration, volume);
    }

    public static void PlayAnnouncer(AudioClip clip, float volume = 1f)
    {
        Ensure().PlayOneShot(clip, Bus.Announcer, volume);
    }

    public static void PlayAmbient(AudioClip clip)
    {
        AudioSettings settings = Ensure();
        settings.EnsureSources();
        if (clip == null)
        {
            settings.ambientSource.Stop();
            settings.ambientSource.clip = null;
            return;
        }

        if (settings.ambientSource.clip == clip && settings.ambientSource.isPlaying)
            return;

        settings.ambientSource.clip = clip;
        settings.ambientSource.loop = true;
        settings.ambientSource.volume = settings.Volume(Bus.Ambient);
        settings.ambientSource.Play();
    }

    public static void StopAmbient()
    {
        if (instance == null || instance.ambientSource == null)
            return;
        instance.ambientSource.Stop();
        instance.ambientSource.clip = null;
    }

    public static void PlayLevelMusic(AudioClip clip)
    {
        AudioSettings settings = Ensure();
        settings.EnsureSources();
        if (clip == null)
        {
            StopLevelMusic();
            return;
        }

        if (settings.musicSource.clip == clip && settings.musicSource.isPlaying)
            return;

        settings.musicSource.clip = clip;
        settings.musicSource.loop = true;
        settings.musicSource.volume = settings.Volume(Bus.Music);
        settings.musicSource.Play();
    }

    public static void StopLevelMusic()
    {
        if (instance == null || instance.musicSource == null)
            return;
        instance.musicSource.Stop();
        instance.musicSource.clip = null;
    }

    void PlayOneShot(AudioClip clip, Bus bus, float volume, float pitch = 1f)
    {
        if (clip == null)
            return;

        EnsureSources();
        oneShotSource.pitch = pitch;
        oneShotSource.PlayOneShot(clip, Scale(bus, volume));
    }

    void EnsureSources()
    {
        if (musicSource == null)
            musicSource = CreateSource("Level Music");
        if (ambientSource == null)
            ambientSource = CreateSource("Ambient");
        if (oneShotSource == null)
            oneShotSource = CreateSource("One Shots");
    }

    AudioSource CreateSource(string label)
    {
        GameObject go = new GameObject(label);
        go.transform.SetParent(transform, false);
        AudioSource src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.loop = false;
        return src;
    }

    void LoadUiClips()
    {
        selectedClip = Resources.Load<AudioClip>("Sounds/Extra/Selected");
        deselectedClip = Resources.Load<AudioClip>("Sounds/Extra/DeSelected");
        selectionClip = Resources.Load<AudioClip>("Sounds/Extra/Selection");
    }

    void ApplyMusicVolume()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicSource.volume = Volume(Bus.Music);
    }

    void ApplyAmbientVolume()
    {
        if (ambientSource != null && ambientSource.isPlaying)
            ambientSource.volume = Volume(Bus.Ambient);
    }

    void LoadPrefs()
    {
        masterVolume = PlayerPrefs.GetFloat(PrefPrefix + "Master", masterVolume);
        musicVolume = PlayerPrefs.GetFloat(PrefPrefix + "Music", musicVolume);
        sfxVolume = PlayerPrefs.GetFloat(PrefPrefix + "Sfx", sfxVolume);
        narrationVolume = PlayerPrefs.GetFloat(PrefPrefix + "Narration", narrationVolume);
        characterEffectsVolume = PlayerPrefs.GetFloat(PrefPrefix + "CharacterEffects", characterEffectsVolume);
        announcerVolume = PlayerPrefs.GetFloat(PrefPrefix + "Announcer", announcerVolume);
        ambientVolume = PlayerPrefs.GetFloat(PrefPrefix + "Ambient", ambientVolume);
        RememberSaved();
    }

    void SaveIfChanged()
    {
        if (Mathf.Approximately(savedMaster, masterVolume)
            && Mathf.Approximately(savedMusic, musicVolume)
            && Mathf.Approximately(savedSfx, sfxVolume)
            && Mathf.Approximately(savedNarration, narrationVolume)
            && Mathf.Approximately(savedCharacter, characterEffectsVolume)
            && Mathf.Approximately(savedAnnouncer, announcerVolume)
            && Mathf.Approximately(savedAmbient, ambientVolume))
            return;

        PlayerPrefs.SetFloat(PrefPrefix + "Master", masterVolume);
        PlayerPrefs.SetFloat(PrefPrefix + "Music", musicVolume);
        PlayerPrefs.SetFloat(PrefPrefix + "Sfx", sfxVolume);
        PlayerPrefs.SetFloat(PrefPrefix + "Narration", narrationVolume);
        PlayerPrefs.SetFloat(PrefPrefix + "CharacterEffects", characterEffectsVolume);
        PlayerPrefs.SetFloat(PrefPrefix + "Announcer", announcerVolume);
        PlayerPrefs.SetFloat(PrefPrefix + "Ambient", ambientVolume);
        PlayerPrefs.Save();
        RememberSaved();
    }

    void RememberSaved()
    {
        savedMaster = masterVolume;
        savedMusic = musicVolume;
        savedSfx = sfxVolume;
        savedNarration = narrationVolume;
        savedCharacter = characterEffectsVolume;
        savedAnnouncer = announcerVolume;
        savedAmbient = ambientVolume;
    }
}
