using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    private const string PrefMusic = "SETTINGS_MUSIC";
    private const string PrefSfx = "SETTINGS_SFX";

    [Header("Background Music")]
    public AudioClip bgm;

    [Header("Player SFX")]
    public AudioClip attack1;
    public AudioClip attack2;
    public AudioClip attack3;
    public AudioClip playerHurt;
    public AudioClip playerDeath;
    public AudioClip playerJump;
    public AudioClip playerRoll;

    [Header("Enemy SFX")]
    public AudioClip enemyHurt;
    public AudioClip enemyDeath;
    public AudioClip enemyAttack;

    [Range(0f, 1f)] public float musicVolume = 0.4f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    AudioSource musicSource;
    AudioSource sfxSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefMusic, musicVolume));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefSfx, sfxVolume));

        musicSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        sfxSource.loop = false;

        RefreshOutputLevels();
    }

    void Start()
    {
        PlayBGM();
    }

    void RefreshOutputLevels()
    {
        musicSource.volume = musicVolume;
        sfxSource.volume = sfxVolume;

        PlayerPrefs.SetFloat(PrefMusic, musicVolume);
        PlayerPrefs.SetFloat(PrefSfx, sfxVolume);
    }

    public void SetMusicLevel01(float linear)
    {
        musicVolume = Mathf.Clamp01(linear);
        if (musicSource != null)
            musicSource.volume = musicVolume;

        PlayerPrefs.SetFloat(PrefMusic, musicVolume);
        PlayerPrefs.Save();
    }

    public void SetSfxLevel01(float linear)
    {
        sfxVolume = Mathf.Clamp01(linear);

        PlayerPrefs.SetFloat(PrefSfx, sfxVolume);
        PlayerPrefs.Save();
    }

    public float GetMusic01() => musicVolume;

    public float GetSfx01() => sfxVolume;

    public void PlayBGM()
    {
        if (bgm == null)
            return;

        musicSource.clip = bgm;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopBGM() => musicSource.Stop();
    public void PauseBGM() => musicSource.Pause();
    public void ResumeBGM() => musicSource.UnPause();

    public void PlayAttack(int combo)
    {
        switch (combo)
        {
            case 1: PlaySFX(attack1); break;
            case 2: PlaySFX(attack2); break;
            case 3: PlaySFX(attack3); break;
        }
    }

    public void PlayPlayerHurt() => PlaySFX(playerHurt);
    public void PlayPlayerDeath() => PlaySFX(playerDeath);
    public void PlayPlayerJump() => PlaySFX(playerJump);
    public void PlayPlayerRoll() => PlaySFX(playerRoll);

    public void PlayEnemyHurt() => PlaySFX(enemyHurt);
    public void PlayEnemyDeath() => PlaySFX(enemyDeath);
    public void PlayEnemyAttack() => PlaySFX(enemyAttack);

    void PlaySFX(AudioClip clip)
    {
        if (clip == null)
            return;

        sfxSource.PlayOneShot(clip, sfxVolume);
    }
}
