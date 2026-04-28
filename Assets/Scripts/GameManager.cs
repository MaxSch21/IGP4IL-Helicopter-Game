using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Start,
        NoPackage,
        Package,
        Delivered,
        GameOver,
        Win
    }

    public static GameManager Instance { get; private set; }

    [Header("Player References")]
    [SerializeField] private HelicopterController helicopterController;
    [SerializeField] private GameObject carriedPackage;

    [Header("Level")]
    [SerializeField] private int levelIndex = 1;
    [SerializeField] private int requiredPackages = 3;
    [SerializeField] private float startDelay = 0.5f;
    [SerializeField] private float deliveryCooldown = 1f;

    [Header("Fuel")]
    [SerializeField] private float maxFuel = 100f;
    [SerializeField] private float fuelConsumptionRate = 5f;

    [SerializeField] private GameState currentState = GameState.Start;

    private bool hasPackage;
    private bool gameStarted;
    private int deliveredPackages;
    private float currentFuel;
    private Coroutine stateRoutine;

    public event Action<int> OnGameStart;
    public event Action<int, int> OnPackageDelivered;
    public event Action<float, float> OnFuelChanged;
    public event Action OnGameOver;
    public event Action OnWin;
    public event Action<GameState> OnStateChanged;

    public GameState CurrentState => currentState;
    public bool HasPackage => hasPackage;
    public int DeliveredPackages => deliveredPackages;
    public int RequiredPackages => requiredPackages;
    public float CurrentFuel => currentFuel;
    public float MaxFuel => maxFuel;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveReferences();
    }

    void Start()
    {
        StartGame();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!ShouldDrainFuel())
            return;

        float consumption = fuelConsumptionRate;
        if (helicopterController != null)
        {
            float verticalSpeed = Mathf.Abs(helicopterController.CurrentVerticalSpeed);
            float maxVertical = Mathf.Max(1f, helicopterController.maxVerticalSpeed);
            consumption *= 1f + verticalSpeed / maxVertical;
        }

        currentFuel = Mathf.Max(0f, currentFuel - consumption * Time.deltaTime);
        OnFuelChanged?.Invoke(currentFuel, maxFuel);

        if (currentFuel <= 0f)
            EnterGameOver("Fuel depleted");
    }

    public void NotifyUIState()
    {
        if (!gameStarted)
            return;

        OnGameStart?.Invoke(requiredPackages);
        OnPackageDelivered?.Invoke(deliveredPackages, requiredPackages);
        OnFuelChanged?.Invoke(currentFuel, maxFuel);

        if (currentState == GameState.GameOver)
            OnGameOver?.Invoke();
        else if (currentState == GameState.Win)
            OnWin?.Invoke();
    }

    public void TryPickupPackage(GameObject packageObject)
    {
        if (currentState != GameState.NoPackage || hasPackage)
            return;

        hasPackage = true;
        SetCarriedPackageVisible(true);

        if (packageObject != null)
            Destroy(packageObject);

        Debug.Log("GameManager: Package picked up");
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

        if (deliveredPackages >= requiredPackages)
        {
            EnterWin();
            return;
        }

        SetState(GameState.Delivered);
        StartStateRoutine(TransitionToNoPackageAfterCooldown());
    }

    public void TriggerGameOver()
    {
        EnterGameOver("Obstacle hit");
    }

    public void RestartGame()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.buildIndex >= 0)
            SceneManager.LoadScene(activeScene.buildIndex);
        else
            SceneManager.LoadScene(activeScene.name);
    }

    private void StartGame()
    {
        ResolveReferences();
        StopStateRoutine();

        deliveredPackages = 0;
        hasPackage = false;
        gameStarted = true;
        currentFuel = maxFuel;

        SetCarriedPackageVisible(false);
        helicopterController?.SetInputEnabled(true);

        Debug.Log($"GameManager: Starting game, required packages: {requiredPackages}");
        SetState(GameState.Start);
        OnGameStart?.Invoke(requiredPackages);
        OnFuelChanged?.Invoke(currentFuel, maxFuel);

        StartStateRoutine(TransitionFromStart());
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

    private void EnterGameOver(string reason)
    {
        if (currentState == GameState.GameOver || currentState == GameState.Win)
            return;

        StopStateRoutine();
        hasPackage = false;
        SetCarriedPackageVisible(false);
        helicopterController?.SetInputEnabled(false);

        Debug.Log($"GameManager: Game over ({reason})");
        SetState(GameState.GameOver);
        OnGameOver?.Invoke();
    }

    private void EnterWin()
    {
        if (currentState == GameState.Win || currentState == GameState.GameOver)
            return;

        StopStateRoutine();
        hasPackage = false;
        SetCarriedPackageVisible(false);
        helicopterController?.SetInputEnabled(false);
        LevelProgress.CompleteLevel(levelIndex);

        Debug.Log("GameManager: Win condition reached");
        SetState(GameState.Win);
        OnWin?.Invoke();
    }

    private bool ShouldDrainFuel()
    {
        return currentState == GameState.NoPackage ||
               currentState == GameState.Package ||
               currentState == GameState.Delivered;
    }

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

    private void SetCarriedPackageVisible(bool visible)
    {
        if (carriedPackage != null)
            carriedPackage.SetActive(visible);
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
}
