using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(AudioSource))]
public class HelicopterServiceStation : MonoBehaviour
{
    public enum ServiceType
    {
        Fuel,
        Repair
    }

    [SerializeField] private ServiceType serviceType = ServiceType.Fuel;
    [SerializeField, Min(0.01f)] private float tickInterval = 1f;
    [SerializeField, Min(0f)] private float fuelPerTick = 10f;
    [SerializeField, Min(1)] private int repairPerTick = 1;
    [SerializeField] private ParticleSystem serviceEffect;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] serviceClips;
    [SerializeField, Range(0.5f, 2f)] private float minPitch = 0.9f;
    [SerializeField, Range(0.5f, 2f)] private float maxPitch = 1.15f;

    private HelicopterController currentHelicopter;
    private Coroutine serviceRoutine;
    private Coroutine audioRoutine;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Reset()
    {
        Collider2D stationCollider = GetComponent<Collider2D>();
        stationCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HelicopterController helicopter = other != null ? other.GetComponentInParent<HelicopterController>() : null;
        if (helicopter == null)
            return;

        currentHelicopter = helicopter;
        StartServiceRoutine();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        HelicopterController helicopter = other != null ? other.GetComponentInParent<HelicopterController>() : null;
        if (helicopter == null || helicopter != currentHelicopter)
            return;

        currentHelicopter = null;
        StopServiceRoutine();
        StopServiceEffect();
        StopServiceAudio();
    }

    private void OnDisable()
    {
        currentHelicopter = null;
        StopServiceRoutine();
        StopServiceEffect();
        StopServiceAudio();
    }

    private void StartServiceRoutine()
    {
        StopServiceRoutine();
        serviceRoutine = StartCoroutine(ServiceLoop());
    }

    private void StopServiceRoutine()
    {
        if (serviceRoutine == null)
            return;

        StopCoroutine(serviceRoutine);
        serviceRoutine = null;
    }

    private IEnumerator ServiceLoop()
    {
        while (currentHelicopter != null)
        {
            ApplyService();
            yield return new WaitForSeconds(tickInterval);
        }

        serviceRoutine = null;
    }

    private void ApplyService()
    {
        GameManager gameManager = GameManager.Instance;
        if (!CanService(gameManager))
        {
            StopServiceEffect();
            StopServiceAudio();
            return;
        }

        if (serviceType == ServiceType.Fuel)
        {
            float fuelBefore = gameManager.CurrentFuel;
            gameManager.AddFuel(fuelPerTick);
            UpdateServiceEffect(gameManager.CurrentFuel > fuelBefore);
            UpdateServiceAudio(gameManager.CurrentFuel > fuelBefore);
            return;
        }

        int conditionBefore = gameManager.CurrentHeliCondition;
        gameManager.RepairHelicopter(repairPerTick);
        UpdateServiceEffect(gameManager.CurrentHeliCondition > conditionBefore);
        UpdateServiceAudio(gameManager.CurrentHeliCondition > conditionBefore);
    }

    private bool CanService(GameManager gameManager)
    {
        if (gameManager == null)
            return false;

        GameManager.GameState state = gameManager.CurrentState;
        return state != GameManager.GameState.FuelDepleted &&
               state != GameManager.GameState.GameOver &&
               state != GameManager.GameState.Win;
    }

    private void UpdateServiceEffect(bool isActivelyServicing)
    {
        if (serviceEffect == null)
            return;

        if (isActivelyServicing)
        {
            if (!serviceEffect.isPlaying)
                serviceEffect.Play();

            return;
        }

        StopServiceEffect();
    }

    private void StopServiceEffect()
    {
        if (serviceEffect == null)
            return;

        serviceEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void UpdateServiceAudio(bool isActivelyServicing)
    {
        if (isActivelyServicing)
        {
            StartServiceAudio();
            return;
        }

        StopServiceAudio();
    }

    private void StartServiceAudio()
    {
        if (audioRoutine != null || audioSource == null || serviceClips == null || serviceClips.Length == 0)
            return;

        audioRoutine = StartCoroutine(ServiceAudioLoop());
    }

    private void StopServiceAudio()
    {
        if (audioRoutine != null)
        {
            StopCoroutine(audioRoutine);
            audioRoutine = null;
        }

        if (audioSource != null)
            audioSource.Stop();
    }

    private IEnumerator ServiceAudioLoop()
    {
        while (true)
        {
            AudioClip clip = serviceClips[Random.Range(0, serviceClips.Length)];
            if (clip == null)
            {
                yield return null;
                continue;
            }

            float pitch = Random.Range(minPitch, maxPitch);
            audioSource.pitch = pitch;
            audioSource.clip = clip;
            audioSource.loop = false;
            audioSource.Play();

            yield return new WaitForSeconds(clip.length / Mathf.Max(0.01f, pitch));
        }
    }
}
