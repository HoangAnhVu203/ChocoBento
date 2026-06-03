using UnityEngine;

public class AudioManager : Singleton<AudioManager>
{
    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Clips")]
    public AudioClip bgmGameplay;
    public AudioClip sfxWin;
    public AudioClip sfxLose;
    public AudioClip sfxButton;
    public AudioClip sfxClick;
    public AudioClip sfxKnifeLift;
    public AudioClip sfxKnifeDrop;
    public AudioClip sfxChocolateLift;
    public AudioClip sfxChocolateDrop;
    public AudioClip sfxChocolateCut;

    private bool musicStarted;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        ApplySettings();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        if (!IsSoundOn()) return;
        if (sfxSource == null) return;

        sfxSource.PlayOneShot(clip);
    }

    public void PlayMusic(AudioClip clip)
    {
        musicStarted = true;
        if (clip == null) return;
        if (!IsMusicOn()) return;
        if (musicSource == null) return;

        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    // ---- helper ----

    public void PlaySoundClickLine() => PlaySFX(sfxClick);
    public void PlayWin() => PlaySFX(sfxWin);
    public void PlayLose() => PlaySFX(sfxLose);
    public void PlayButton() => PlaySFX(sfxButton);
    public void PlayBackgroundMusic()
    {
        var clip = bgmGameplay != null ? bgmGameplay : musicSource != null ? musicSource.clip : null;
        PlayMusic(clip);
    }
    public void PlayKnifeLift() => PlaySFX(sfxKnifeLift);
    public void PlayKnifeDrop() => PlaySFX(sfxKnifeDrop);
    public void PlayChocolateLift() => PlaySFX(sfxChocolateLift);
    public void PlayChocolateDrop() => PlaySFX(sfxChocolateDrop);
    public void PlayChocolateCut() => PlaySFX(sfxChocolateCut);

    //---------------- SETTING ----------------//
    public static bool IsSoundOn() => PlayerPrefs.GetInt("SOUND_ON", 1) == 1;
    public static bool IsMusicOn() => PlayerPrefs.GetInt("MUSIC_ON", 1) == 1;
    public static bool IsVibrationOn() => PlayerPrefs.GetInt("VIBRATION_ON", 1) == 1;

    public void ApplySettings()
    {
        ApplySoundSetting(IsSoundOn());
        ApplyMusicSetting(IsMusicOn());
    }

    public void SetSoundOn(bool isOn)
    {
        PlayerPrefs.SetInt("SOUND_ON", isOn ? 1 : 0);
        PlayerPrefs.Save();
        ApplySoundSetting(isOn);
    }

    public void SetMusicOn(bool isOn)
    {
        PlayerPrefs.SetInt("MUSIC_ON", isOn ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMusicSetting(isOn);
    }

    public static void SetVibrationOn(bool isOn)
    {
        PlayerPrefs.SetInt("VIBRATION_ON", isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplySoundSetting(bool isOn)
    {
        if (sfxSource == null) return;

        sfxSource.mute = !isOn;

        if (!isOn)
            sfxSource.Stop();
    }

    private void ApplyMusicSetting(bool isOn)
    {
        if (musicSource == null) return;

        musicSource.mute = !isOn;

        if (isOn && musicStarted)
        {
            if (musicSource.clip == null)
                musicSource.clip = bgmGameplay;

            if (musicSource.clip != null && !musicSource.isPlaying)
            {
                musicSource.loop = true;
                musicSource.Play();
            }
        }
        else
        {
            musicSource.Stop();
        }
    }
}
