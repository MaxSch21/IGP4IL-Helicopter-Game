using UnityEngine;
using UnityEngine.Serialization;

public class PlayerDamageVFXController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private HelicopterController helicopterController;
    [FormerlySerializedAs("damagedEffect")]
    [SerializeField] private ParticleSystem damagedSmokeEffect;
    [SerializeField] private ParticleSystem crashEffect;
    [Header("Emission")]
    [SerializeField, Min(0f)] private float maxVerticalSpeedForFullEmission = 8f;
    [SerializeField, Min(0f)] private float minEmissionRate = 0f;
    [SerializeField, Min(0f)] private float maxEmissionRate = 30f;

    private ParticleSystem.EmissionModule emissionModule;
    private bool hasEmissionModule;
    private ParticleSystem[] playerEffects;

    private void Awake()
    {
        ResolveReferences();

        if (damagedSmokeEffect == null)
            damagedSmokeEffect = FindDamagedEffect();

        if (playerEffects == null || playerEffects.Length == 0)
            playerEffects = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void OnEnable()
    {
        BindEvents();
    }

    private void Start()
    {
        BindEvents();
    }

    private void OnDisable()
    {
        UnbindEvents();
        StopAllEffects();
    }

    private void Update()
    {
        UpdateSmokeEmissionFromVerticalSpeed();
    }

    private void BindEvents()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();

        if (gameManager == null)
            return;

        gameManager.OnHeliConditionChanged -= HandleHeliConditionChanged;
        gameManager.OnHeliConditionChanged += HandleHeliConditionChanged;
        gameManager.OnGameStart -= HandleGameStart;
        gameManager.OnGameStart += HandleGameStart;
        gameManager.OnGameOver -= HandleGameOver;
        gameManager.OnGameOver += HandleGameOver;
        gameManager.OnWin -= HandleWin;
        gameManager.OnWin += HandleWin;

        HandleHeliConditionChanged(gameManager.CurrentHeliCondition, gameManager.MaxHeliCondition);
    }

    private void UnbindEvents()
    {
        if (gameManager == null)
            return;

        gameManager.OnHeliConditionChanged -= HandleHeliConditionChanged;
        gameManager.OnGameStart -= HandleGameStart;
        gameManager.OnGameOver -= HandleGameOver;
        gameManager.OnWin -= HandleWin;
    }

    private void HandleHeliConditionChanged(int current, int max)
    {
        if (damagedSmokeEffect == null)
            damagedSmokeEffect = FindDamagedEffect();

        if (damagedSmokeEffect == null)
            return;

        if (current < max)
            PlaySmokeEffect();
        else
            StopSmokeEffect();
    }

    private void HandleGameStart(int _)
    {
        StopAllEffects();
    }

    private void HandleGameOver()
    {
        PlayCrashEffect();
    }

    private void HandleWin()
    {
        StopAllEffects();
    }

    private void PlaySmokeEffect()
    {
        if (damagedSmokeEffect == null)
            return;

        if (!damagedSmokeEffect.gameObject.activeSelf)
            damagedSmokeEffect.gameObject.SetActive(true);

        if (!damagedSmokeEffect.isPlaying)
            damagedSmokeEffect.Play(true);

        UpdateSmokeEmissionFromVerticalSpeed();
    }

    private void StopSmokeEffect()
    {
        if (damagedSmokeEffect == null)
            return;

        damagedSmokeEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (damagedSmokeEffect.gameObject.activeSelf)
            damagedSmokeEffect.gameObject.SetActive(false);
    }

    private void PlayCrashEffect()
    {
        if (crashEffect == null)
            return;

        if (!crashEffect.gameObject.activeSelf)
            crashEffect.gameObject.SetActive(true);

        if (!crashEffect.isPlaying)
            crashEffect.Play(true);
    }

    private void StopAllEffects()
    {
        if (playerEffects == null || playerEffects.Length == 0)
            playerEffects = GetComponentsInChildren<ParticleSystem>(true);

        if (playerEffects == null)
            return;

        foreach (ParticleSystem effect in playerEffects)
        {
            if (effect == null)
                continue;

            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (effect.gameObject.activeSelf)
                effect.gameObject.SetActive(false);
        }
    }

    private void UpdateSmokeEmissionFromVerticalSpeed()
    {
        if (damagedSmokeEffect == null || !damagedSmokeEffect.gameObject.activeSelf)
            return;

        if (helicopterController == null)
            helicopterController = FindFirstObjectByType<HelicopterController>();

        if (helicopterController == null)
            return;

        float verticalSpeed = Mathf.Abs(helicopterController.CurrentVerticalSpeed);
        float t = Mathf.InverseLerp(0f, maxVerticalSpeedForFullEmission, verticalSpeed);
        float emissionRate = Mathf.Lerp(minEmissionRate, maxEmissionRate, t);

        if (!hasEmissionModule)
        {
            emissionModule = damagedSmokeEffect.emission;
            hasEmissionModule = true;
        }

        emissionModule.rateOverTime = emissionRate;
    }

    private void ResolveReferences()
    {
        if (helicopterController == null)
            helicopterController = FindFirstObjectByType<HelicopterController>();
    }

    private ParticleSystem FindDamagedEffect()
    {
        Transform effectTransform = transform.Find("PS_Damaged");
        if (effectTransform != null && effectTransform.TryGetComponent(out ParticleSystem effect))
            return effect;

        return GetComponentInChildren<ParticleSystem>(true);
    }
}
