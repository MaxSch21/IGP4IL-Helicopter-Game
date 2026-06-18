using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    private const string MusicMutedKey = "MusicMuted";
    private const string SfxMutedKey = "SfxMuted";
    private const string MusicVolumeParameter = "MusicVolume";
    private const string SfxVolumeParameter = "SfxVolume";
    private const float MutedVolume = -80f;
    private const float UnmutedVolume = 0f;

    [Header("Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioSource helicopterSource;
    [SerializeField] private AudioSource lowFuelSource;
    [SerializeField] private AudioSource fallSource;

    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("SFX")]
    [SerializeField] private AudioClip winClip;
    [SerializeField] private AudioClip gameOverClip;
    [SerializeField] private AudioClip[] heliCrashClips;
    [SerializeField] private AudioClip explosionClip;
    [SerializeField] private AudioClip cratePickedUpClip;
    [SerializeField] private AudioClip crateDeliveredClip;

    [Header("Loops")]
    [SerializeField] private AudioClip ambientClip;
    [SerializeField] private AudioClip helicopterClip;
    [SerializeField] private AudioClip lowFuelClip;
    [SerializeField] private AudioClip fallClip;

    [Header("Helicopter Pitch")]
    [SerializeField] private HelicopterController helicopterController;
    [SerializeField, Range(0.5f, 2f)] private float minHelicopterPitch = 0.9f;
    [SerializeField, Range(0.5f, 2f)] private float maxHelicopterPitch = 1.15f;
    [SerializeField, Min(0f)] private float speedPitchAmount = 0.15f;
    [SerializeField, Min(0.01f)] private float pitchUpdateInterval = 0.2f;

    [Header("Low Fuel")]
    [SerializeField, Range(0f, 1f)] private float lowFuelThreshold = 0.2f;

    private GameManager gameManager;
    private float nextPitchUpdateTime;

    private void Awake()
    {
        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        ambientSource = EnsureSource(ambientSource);
        helicopterSource = EnsureSource(helicopterSource);
        lowFuelSource = EnsureSource(lowFuelSource);
        fallSource = EnsureSource(fallSource);

        if (helicopterController == null)
            helicopterController = FindFirstObjectByType<HelicopterController>();

        SettingsMenuController.ApplySessionDefaultsOnce();
        ApplySavedMixerSettings();

        ConfigureLoopSource(ambientSource, ambientClip);
        ConfigureLoopSource(helicopterSource, helicopterClip);
        ConfigureLoopSource(lowFuelSource, lowFuelClip);
        ConfigureLoopSource(fallSource, fallClip);
    }

    private void OnEnable()
    {
        Subscribe();
        PlayLoop(ambientSource);
    }

    private void Start()
    {
        Subscribe();
    }

    private void Update()
    {
        UpdateHelicopterPitch();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (gameManager != null)
            return;

        gameManager = GameManager.Instance;
        if (gameManager == null)
            return;

        gameManager.OnGameStart += OnGameStart;
        gameManager.OnPackagePickedUp += OnPackagePickedUp;
        gameManager.OnPackageDelivered += OnPackageDelivered;
        gameManager.OnFuelChanged += OnFuelChanged;
        gameManager.OnGameOver += OnGameOver;
        gameManager.OnWin += OnWin;
        gameManager.OnStateChanged += OnStateChanged;
        gameManager.OnHeliDestroyed += OnHeliDestroyed;

        SyncAudioToState();
    }

    private void Unsubscribe()
    {
        if (gameManager == null)
            return;

        gameManager.OnGameStart -= OnGameStart;
        gameManager.OnPackagePickedUp -= OnPackagePickedUp;
        gameManager.OnPackageDelivered -= OnPackageDelivered;
        gameManager.OnFuelChanged -= OnFuelChanged;
        gameManager.OnGameOver -= OnGameOver;
        gameManager.OnWin -= OnWin;
        gameManager.OnStateChanged -= OnStateChanged;
        gameManager.OnHeliDestroyed -= OnHeliDestroyed;
        gameManager = null;
    }

    private void OnGameStart(int maxPackages)
    {
        StopLoop(lowFuelSource);
        StopLoop(fallSource);
        PlayLoop(helicopterSource);
    }

    private void SyncAudioToState()
    {
        if (gameManager == null)
            return;

        OnStateChanged(gameManager.CurrentState);
        OnFuelChanged(gameManager.CurrentFuel, gameManager.MaxFuel);
    }

    private void OnPackagePickedUp()
    {
        PlayOneShot(cratePickedUpClip);
    }

    private void OnPackageDelivered(int current, int max)
    {
        PlayOneShot(crateDeliveredClip);
    }

    private void OnFuelChanged(float current, float max)
    {
        if (max <= 0f || gameManager == null || IsTerminalOrFuelDepleted())
        {
            StopLoop(lowFuelSource);
            return;
        }

        if (current > 0f && current <= max * lowFuelThreshold)
            PlayLoop(lowFuelSource);
        else
            StopLoop(lowFuelSource);
    }

    private void OnStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.NoPackage ||
            state == GameManager.GameState.Package ||
            state == GameManager.GameState.Delivered)
        {
            PlayLoop(helicopterSource);
            return;
        }

        if (state == GameManager.GameState.FuelDepleted)
        {
            StopLoop(helicopterSource);
            StopLoop(lowFuelSource);
            PlayLoop(fallSource);
        }
    }

    private void OnGameOver()
    {
        StopLoop(helicopterSource);
        StopLoop(lowFuelSource);
        StopLoop(fallSource);
        PlayOneShot(gameOverClip);
    }

    private void OnWin()
    {
        StopLoop(helicopterSource);
        StopLoop(lowFuelSource);
        StopLoop(fallSource);
        PlayOneShot(winClip);
    }

    private void OnHeliDestroyed()
    {
        PlayRandomOneShot(heliCrashClips);
        PlayOneShot(explosionClip);
    }

    private void UpdateHelicopterPitch()
    {
        if (helicopterSource == null || !helicopterSource.isPlaying)
            return;

        if (Time.time < nextPitchUpdateTime)
            return;

        nextPitchUpdateTime = Time.time + pitchUpdateInterval;

        float speedPitch = 0f;
        if (helicopterController != null)
        {
            float maxSpeed = Mathf.Max(1f, helicopterController.maxVerticalSpeed);
            speedPitch = Mathf.Abs(helicopterController.CurrentVerticalSpeed) / maxSpeed * speedPitchAmount;
        }

        helicopterSource.pitch = Random.Range(minHelicopterPitch, maxHelicopterPitch) + speedPitch;
    }

    private bool IsTerminalOrFuelDepleted()
    {
        return gameManager.CurrentState == GameManager.GameState.FuelDepleted ||
               gameManager.CurrentState == GameManager.GameState.GameOver ||
               gameManager.CurrentState == GameManager.GameState.Win;
    }

    private void ConfigureLoopSource(AudioSource source, AudioClip clip)
    {
        if (source == null)
            return;

        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
    }

    private void ApplySavedMixerSettings()
    {
        AudioMixer mixer = GetAudioMixer();
        if (mixer == null)
            return;

        SetMixerVolume(mixer, MusicVolumeParameter, GetBool(MusicMutedKey));
        SetMixerVolume(mixer, SfxVolumeParameter, GetBool(SfxMutedKey));
    }

    private AudioMixer GetAudioMixer()
    {
        if (audioMixer != null)
            return audioMixer;

        AudioSource[] sources = { sfxSource, ambientSource, helicopterSource, lowFuelSource, fallSource };
        foreach (AudioSource source in sources)
        {
            if (source != null && source.outputAudioMixerGroup != null)
                return source.outputAudioMixerGroup.audioMixer;
        }

        return null;
    }

    private void SetMixerVolume(AudioMixer mixer, string parameterName, bool muted)
    {
        mixer.SetFloat(parameterName, muted ? MutedVolume : UnmutedVolume);
    }

    private bool GetBool(string key)
    {
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    private AudioSource EnsureSource(AudioSource source)
    {
        if (source != null)
            return source;

        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        newSource.playOnAwake = false;
        newSource.loop = true;
        return newSource;
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip);
    }

    private void PlayRandomOneShot(AudioClip[] clips)
    {
        AudioClip clip = GetRandomClip(clips);
        PlayOneShot(clip);
    }

    private AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
            return null;

        return clips[Random.Range(0, clips.Length)];
    }

    private void PlayLoop(AudioSource source)
    {
        if (source == null || source.clip == null || source.isPlaying)
            return;

        source.Play();
    }

    private void StopLoop(AudioSource source)
    {
        if (source != null && source.isPlaying)
            source.Stop();
    }
}
