using UnityEngine;

public class PlayerDamageVFXController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private HelicopterController helicopterController;
    [SerializeField] private ParticleSystem damagedEffect;
    [Header("Emission")]
    [SerializeField, Min(0f)] private float maxVerticalSpeedForFullEmission = 8f;
    [SerializeField, Min(0f)] private float minEmissionRate = 0f;
    [SerializeField, Min(0f)] private float maxEmissionRate = 30f;

    private ParticleSystem.EmissionModule emissionModule;
    private bool hasEmissionModule;

    private void Awake()
    {
        ResolveReferences();

        if (damagedEffect == null)
            damagedEffect = FindDamagedEffect();
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
        StopEffect();
    }

    private void Update()
    {
        UpdateEmissionRate();
    }

    private void BindEvents()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();

        if (gameManager == null)
            return;

        gameManager.OnHeliConditionChanged -= HandleHeliConditionChanged;
        gameManager.OnHeliConditionChanged += HandleHeliConditionChanged;
        gameManager.OnGameStart -= HandleGameResetStart;
        gameManager.OnGameStart += HandleGameResetStart;
        gameManager.OnGameOver -= HandleGameResetImmediate;
        gameManager.OnGameOver += HandleGameResetImmediate;
        gameManager.OnWin -= HandleGameResetImmediate;
        gameManager.OnWin += HandleGameResetImmediate;

        HandleHeliConditionChanged(gameManager.CurrentHeliCondition, gameManager.MaxHeliCondition);
    }

    private void UnbindEvents()
    {
        if (gameManager == null)
            return;

        gameManager.OnHeliConditionChanged -= HandleHeliConditionChanged;
        gameManager.OnGameStart -= HandleGameResetStart;
        gameManager.OnGameOver -= HandleGameResetImmediate;
        gameManager.OnWin -= HandleGameResetImmediate;
    }

    private void HandleHeliConditionChanged(int current, int max)
    {
        if (damagedEffect == null)
            damagedEffect = FindDamagedEffect();

        if (damagedEffect == null)
            return;

        if (current < max)
            PlayEffect();
        else
            StopEffect();
    }

    private void HandleGameResetStart(int _)
    {
        StopEffect();
    }

    private void HandleGameResetImmediate()
    {
        StopEffect();
    }

    private void PlayEffect()
    {
        if (damagedEffect == null)
            return;

        if (!damagedEffect.gameObject.activeSelf)
            damagedEffect.gameObject.SetActive(true);

        if (!damagedEffect.isPlaying)
            damagedEffect.Play(true);

        UpdateEmissionRate();
    }

    private void StopEffect()
    {
        if (damagedEffect == null)
            return;

        damagedEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (damagedEffect.gameObject.activeSelf)
            damagedEffect.gameObject.SetActive(false);
    }

    private void UpdateEmissionRate()
    {
        if (damagedEffect == null || !damagedEffect.gameObject.activeSelf)
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
            emissionModule = damagedEffect.emission;
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
