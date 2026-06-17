using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private const int StarBonusPoints = 100;

    public enum GameState
    {
        Start,
        NoPackage,
        Package,
        Delivered,
        FuelDepleted,
        GameOver,
        Win
    }

    // Singleton / References
    public static GameManager Instance { get; private set; }

    [Header("Player References")]
    [SerializeField] private HelicopterController helicopterController;
    [SerializeField] private GameObject carriedPackage;

    // Serialized Settings
    [Header("Level")]
    [SerializeField] private int levelIndex = 1;
    [SerializeField] private string nextLevelSceneName;
    [SerializeField] private int requiredPackages = 3;
    [SerializeField] private float startDelay = 0.5f;
    [SerializeField] private float deliveryCooldown = 1f;
    [SerializeField] private float estimatedTimeSeconds = 60f;

    [Header("Fuel")]
    [SerializeField] private float maxFuel = 100f;
    [SerializeField] private float fuelConsumptionRate = 5f;

    [Header("Helicopter Damage")]
    [SerializeField] private int maxHeliCondition = 4;
    [SerializeField] private float damageCooldown = 1f;

    [SerializeField] private GameState currentState = GameState.Start;

    // Runtime State
    private bool hasPackage;
    private bool gameStarted;
    private int deliveredPackages;
    private int heliCondition;
    private float currentFuel;
    private bool fuelDepleted;
    private bool canTakeDamage = true;
    private Coroutine stateRoutine;
    private Coroutine damageCooldownRoutine;
    private float levelStartTime;
    private float levelEndTime;
    private LevelResult lastLevelResult;

    // Events
    public event Action<int> OnGameStart;
    public event Action<int, int> OnPackageDelivered;
    public event Action<float, float> OnFuelChanged;
    public event Action OnGameOver;
    public event Action OnWin;
    public event Action OnPackagePickedUp;
    public event Action<GameState> OnStateChanged;
    public event Action<int, int> OnHeliConditionChanged;
    public event Action OnHeliDestroyed;

    public GameState CurrentState => currentState;
    public bool HasPackage => hasPackage;
    public bool IsFuelDepleted => fuelDepleted;
    public int DeliveredPackages => deliveredPackages;
    public int RequiredPackages => requiredPackages;
    public int CurrentHeliCondition => heliCondition;
    public int MaxHeliCondition => maxHeliCondition;
    public float CurrentFuel => currentFuel;
    public float MaxFuel => maxFuel;
    public float EstimatedTimeSeconds => estimatedTimeSeconds;
    public float ElapsedTime => gameStarted ? Mathf.Max(0f, GetLevelTimeReference() - levelStartTime) : 0f;
    public LevelResult LastLevelResult => lastLevelResult;
    public bool HasNextLevel => !string.IsNullOrWhiteSpace(nextLevelSceneName) || HasNextSceneInBuildSettings();

    // Unity Lifecycle
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveReferences();
        ApplyLevelRuntimeConfiguration();
    }

    private void Start()
    {
        StartGame();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!ShouldDrainFuel())
            return;

        DrainFuelOverTime();
    }

    // Level Setup / Restart
    public void NotifyUIState()
    {
        if (!gameStarted)
            return;

        OnGameStart?.Invoke(requiredPackages);
        OnPackageDelivered?.Invoke(deliveredPackages, requiredPackages);
        OnFuelChanged?.Invoke(currentFuel, maxFuel);
        OnHeliConditionChanged?.Invoke(heliCondition, maxHeliCondition);

        if (currentState == GameState.GameOver)
            OnGameOver?.Invoke();
        else if (currentState == GameState.Win)
            OnWin?.Invoke();
    }

    public void RestartGame()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.buildIndex >= 0)
            SceneManager.LoadScene(activeScene.buildIndex);
        else
            SceneManager.LoadScene(activeScene.name);
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrWhiteSpace(nextLevelSceneName))
        {
            LevelRuntime.Clear();
            SceneManager.LoadScene(nextLevelSceneName);
            return;
        }

        if (TryLoadNextSceneInBuildSettings())
            return;

        Debug.LogWarning("GameManager: No next level configured.");
    }

    private void StartGame()
    {
        ResolveReferences();
        ApplyLevelRuntimeConfiguration();
        StopStateRoutine();
        StopDamageCooldownRoutine();
        ScoreManager.Instance?.ResetScore();

        deliveredPackages = 0;
        hasPackage = false;
        gameStarted = true;
        currentFuel = maxFuel;
        heliCondition = maxHeliCondition;
        fuelDepleted = false;
        canTakeDamage = true;
        levelStartTime = Time.time;
        levelEndTime = 0f;
        lastLevelResult = default;

        SetCarriedPackageVisible(false);
        SetPlayerActiveState(true, false);

        Debug.Log($"GameManager: Starting game, required packages: {requiredPackages}");
        SetState(GameState.Start);
        OnGameStart?.Invoke(requiredPackages);
        OnFuelChanged?.Invoke(currentFuel, maxFuel);
        OnHeliConditionChanged?.Invoke(heliCondition, maxHeliCondition);

        StartStateRoutine(TransitionFromStart());
    }

    private void ApplyLevelRuntimeConfiguration()
    {
        if (!LevelRuntime.HasLevelData || !LevelRuntime.Current.IsValid)
            return;

        LevelRuntimeData runtime = LevelRuntime.Current;
        levelIndex = runtime.LevelIndex;
        requiredPackages = runtime.RequiredPackages;
        maxFuel = runtime.StartFuel;
        estimatedTimeSeconds = runtime.EstimatedTimeSeconds;
    }

    private void ResolveReferences()
    {
        if (helicopterController == null)
            helicopterController = FindFirstObjectByType<HelicopterController>();

        if (carriedPackage == null && helicopterController != null)
        {
            Transform packageTransform = helicopterController.transform.Find("carriedPackage");
            if (packageTransform == null)
                packageTransform = helicopterController.transform.Find("Held Package");

            if (packageTransform != null)
                carriedPackage = packageTransform.gameObject;
        }
    }

    private bool TryLoadNextSceneInBuildSettings()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        int nextBuildIndex = activeScene.buildIndex + 1;

        if (activeScene.buildIndex < 0 || nextBuildIndex >= SceneManager.sceneCountInBuildSettings)
            return false;

        LevelRuntime.Clear();
        SceneManager.LoadScene(nextBuildIndex);
        return true;
    }

    private bool HasNextSceneInBuildSettings()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        int nextBuildIndex = activeScene.buildIndex + 1;
        return activeScene.buildIndex >= 0 && nextBuildIndex < SceneManager.sceneCountInBuildSettings;
    }

    // State Changes
    private void SetState(GameState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;
        Debug.Log($"GameManager: State changed to {newState}");
        OnStateChanged?.Invoke(newState);
    }

    private void StartStateRoutine(IEnumerator routine)
    {
        StopStateRoutine();
        stateRoutine = StartCoroutine(routine);
    }

    private void StopStateRoutine()
    {
        if (stateRoutine == null)
            return;

        StopCoroutine(stateRoutine);
        stateRoutine = null;
    }

    private IEnumerator TransitionFromStart()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        SetState(GameState.NoPackage);
    }

    private IEnumerator TransitionToNoPackageAfterCooldown()
    {
        if (deliveryCooldown > 0f)
            yield return new WaitForSeconds(deliveryCooldown);

        if (currentState == GameState.Delivered)
            SetState(GameState.NoPackage);
    }

    // Package Logic
    public void TryPickupPackage(GameObject packageObject)
    {
        if (currentState != GameState.NoPackage || hasPackage)
            return;

        hasPackage = true;
        SetCarriedPackageVisible(true);

        if (packageObject != null)
            Destroy(packageObject);

        Debug.Log("GameManager: Package picked up");
        OnPackagePickedUp?.Invoke();
        SetState(GameState.Package);
    }

    public void TryDeliverPackage()
    {
        if (currentState != GameState.Package || !hasPackage)
            return;

        hasPackage = false;
        deliveredPackages++;
        SetCarriedPackageVisible(false);

        Debug.Log($"GameManager: Package delivered ({deliveredPackages}/{requiredPackages})");
        OnPackageDelivered?.Invoke(deliveredPackages, requiredPackages);
        ScoreManager.Instance?.AddPackageScore();

        if (deliveredPackages >= requiredPackages)
        {
            EnterWin();
            return;
        }

        SetState(GameState.Delivered);
        StartStateRoutine(TransitionToNoPackageAfterCooldown());
    }

    private void SetCarriedPackageVisible(bool visible)
    {
        if (carriedPackage != null)
            carriedPackage.SetActive(visible);
    }

    // Fuel Logic
    public void AddFuel(float amount)
    {
        if (amount <= 0f || IsInTerminalState())
            return;

        currentFuel = Mathf.Min(maxFuel, currentFuel + amount);
        OnFuelChanged?.Invoke(currentFuel, maxFuel);

        if (fuelDepleted && currentState == GameState.FuelDepleted && currentFuel > 0f)
            Debug.Log($"GameManager: Fuel restored to {currentFuel}/{maxFuel}");
    }

    public void RepairHelicopter(int amount = 1)
    {
        if (amount <= 0 || IsInTerminalState())
            return;

        int newCondition = Mathf.Min(maxHeliCondition, heliCondition + amount);
        if (newCondition == heliCondition)
            return;

        heliCondition = newCondition;
        OnHeliConditionChanged?.Invoke(heliCondition, maxHeliCondition);
    }

    private void DrainFuelOverTime()
    {
        float consumption = fuelConsumptionRate;
        if (helicopterController != null)
        {
            float verticalSpeed = Mathf.Abs(helicopterController.CurrentVerticalSpeed);
            float maxVertical = Mathf.Max(1f, helicopterController.maxVerticalSpeed);
            consumption *= 1f + verticalSpeed / maxVertical;
        }

        currentFuel = Mathf.Max(0f, currentFuel - consumption * Time.deltaTime);
        OnFuelChanged?.Invoke(currentFuel, maxFuel);

        if (currentFuel <= 0f && !fuelDepleted)
            EnterFuelDepleted();
    }

    private bool ShouldDrainFuel()
    {
        return currentState == GameState.NoPackage ||
               currentState == GameState.Package ||
               currentState == GameState.Delivered;
    }

    // Damage Logic
    public void TriggerGameOver()
    {
        EnterGameOver("Obstacle hit");
    }

    public void TriggerGameOver(string reason)
    {
        EnterGameOver(reason);
    }

    public void TryFuelDepletedGroundHit()
    {
        if (!fuelDepleted || currentState != GameState.FuelDepleted)
            return;

        EnterGameOver("Fuel depleted ground hit", true);
    }

    public void TakeRotorHit()
    {
        OnHeliDestroyed?.Invoke();
        TriggerGameOver("Rotor hit");
    }

    public void TakeBodyHit()
    {
        if (!canTakeDamage)
            return;

        canTakeDamage = false;
        heliCondition = Mathf.Max(0, heliCondition - 1);

        Debug.Log($"GameManager: Helicopter condition {heliCondition}/{maxHeliCondition}");
        OnHeliConditionChanged?.Invoke(heliCondition, maxHeliCondition);

        if (heliCondition <= 0)
        {
            OnHeliDestroyed?.Invoke();
            TriggerGameOver("Body destroyed");
            return;
        }

        StartDamageCooldownRoutine();
    }

    private void StartDamageCooldownRoutine()
    {
        StopDamageCooldownRoutine();

        if (damageCooldown <= 0f)
        {
            canTakeDamage = true;
            return;
        }

        damageCooldownRoutine = StartCoroutine(DamageCooldown());
    }

    private IEnumerator DamageCooldown()
    {
        yield return new WaitForSeconds(damageCooldown);
        canTakeDamage = true;
        damageCooldownRoutine = null;
    }

    private void StopDamageCooldownRoutine()
    {
        if (damageCooldownRoutine == null)
            return;

        StopCoroutine(damageCooldownRoutine);
        damageCooldownRoutine = null;
        canTakeDamage = true;
    }

    // Win / GameOver Logic
    private void EnterFuelDepleted()
    {
        if (IsInTerminalState() || fuelDepleted)
            return;

        fuelDepleted = true;
        StopStateRoutine();
        StopDamageCooldownRoutine();
        SetPlayerActiveState(false, true);

        Debug.Log("GameManager: Fuel depleted, crash mode enabled");
        SetState(GameState.FuelDepleted);
    }

    private void EnterGameOver(string reason, bool fuelDepleted = false)
    {
        if (IsInTerminalState())
            return;

        levelEndTime = Time.time;
        StopStateRoutine();
        StopDamageCooldownRoutine();
        hasPackage = false;
        SetCarriedPackageVisible(false);
        SetPlayerActiveState(false, fuelDepleted);

        Debug.Log($"GameManager: Game over ({reason})");
        SetState(GameState.GameOver);
        OnGameOver?.Invoke();
    }

    private void EnterWin()
    {
        if (IsInTerminalState())
            return;

        levelEndTime = Time.time;
        StopStateRoutine();
        StopDamageCooldownRoutine();
        hasPackage = false;
        SetCarriedPackageVisible(false);
        SetPlayerActiveState(false, false);
        LevelProgress.CompleteLevel(levelIndex);
        lastLevelResult = BuildLevelResult();
        int earnedStars = StarEvaluator.Evaluate(lastLevelResult);

        Debug.Log("GameManager: Win condition reached");
        SetState(GameState.Win);
        ScoreManager.Instance?.FinalizeScore((int)currentFuel, earnedStars * StarBonusPoints);
        OnWin?.Invoke();
    }

    private void SetPlayerActiveState(bool inputEnabled, bool crashModeEnabled)
    {
        helicopterController?.SetCrashMode(crashModeEnabled);
        helicopterController?.SetInputEnabled(inputEnabled);
    }

    private bool IsInTerminalState()
    {
        return currentState == GameState.GameOver || currentState == GameState.Win;
    }

    private LevelResult BuildLevelResult()
    {
        return new LevelResult
        {
            LevelIndex = levelIndex,
            PackagesDelivered = deliveredPackages,
            RequiredPackages = requiredPackages,
            DamageTaken = Mathf.Max(0, maxHeliCondition - heliCondition),
            FuelRemaining = currentFuel,
            ElapsedSeconds = ElapsedTime,
            EstimatedTimeSeconds = estimatedTimeSeconds
        };
    }

    private float GetLevelTimeReference()
    {
        return levelEndTime > 0f ? levelEndTime : Time.time;
    }
}
