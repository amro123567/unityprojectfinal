using UnityEngine;

// Attach to a persistent empty GameObject called "AudioManager"
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

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

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float musicVolume = 0.4f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    void Awake()
    {
        // Singleton — persists across scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Create two AudioSource components
        // One for looping music, one for SFX
        musicSource = gameObject.AddComponent<AudioSource>();
        sfxSource   = gameObject.AddComponent<AudioSource>();

        musicSource.loop   = true;
        musicSource.volume = musicVolume;

        sfxSource.loop   = false;
        sfxSource.volume = sfxVolume;
    }

    void Start()
    {
        PlayBGM();
    }

    // ── Music ─────────────────────────────────────────

    public void PlayBGM()
    {
        if (bgm == null) return;
        musicSource.clip = bgm;
        musicSource.Play();
    }

    public void StopBGM()   => musicSource.Stop();
    public void PauseBGM()  => musicSource.Pause();
    public void ResumeBGM() => musicSource.UnPause();

    // ── Player SFX ───────────────────────────────────

    public void PlayAttack(int combo)
    {
        switch (combo)
        {
            case 1: PlaySFX(attack1); break;
            case 2: PlaySFX(attack2); break;
            case 3: PlaySFX(attack3); break;
        }
    }

    public void PlayPlayerHurt()  => PlaySFX(playerHurt);
    public void PlayPlayerDeath() => PlaySFX(playerDeath);
    public void PlayPlayerJump()  => PlaySFX(playerJump);
    public void PlayPlayerRoll()  => PlaySFX(playerRoll);

    // ── Enemy SFX ────────────────────────────────────

    public void PlayEnemyHurt()   => PlaySFX(enemyHurt);
    public void PlayEnemyDeath()  => PlaySFX(enemyDeath);
    public void PlayEnemyAttack() => PlaySFX(enemyAttack);

    // ── Core helper ──────────────────────────────────

    void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }
}
