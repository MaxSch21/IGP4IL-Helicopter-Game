using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private float maxFuel = 100f;
    [SerializeField] private float fuelConsumptionRate = 5f;

    private readonly Dictionary<int, int> levelDeliveryGoals = new Dictionary<int, int> { { 0, 3 } };
    private int currentLevel = 0;
    private int currentDeliveries;
    private float currentFuel;
    private bool isGameOver;
    private bool isWin;
    private bool subscribedToCollisionManager;

    public event Action<int, int> PackageDelivered;
    public event Action GameOver;
    public event Action Win;
    public event Action<int> GameStart;
    public event Action<float, float> FuelChanged;

    public void NotifyUIState()
    {
        int target = GetDeliveryTarget(currentLevel);
        GameStart?.Invoke(target);
        if (currentDeliveries > 0)
        {
            PackageDelivered?.Invoke(currentDeliveries, target);
        }
        FuelChanged?.Invoke(currentFuel, maxFuel);
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnEnable()
    {
        SubscribeToCollisionManager();
    }

    void Start()
    {
        InitializeLevel(currentLevel);
        SubscribeToCollisionManager();
    }

    void OnDisable()
    {
        UnsubscribeFromCollisionManager();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        UnsubscribeFromCollisionManager();
    }

    void Update()
    {
        if (isGameOver || isWin)
            return;

        float consumption = fuelConsumptionRate;
        var controller = StateMachine.Instance?.character;
        if (controller != null)
        {
            float verticalSpeed = Mathf.Abs(controller.CurrentVerticalSpeed);
            float maxVertical = Mathf.Max(1f, controller.maxVerticalSpeed);
            float verticalFactor = verticalSpeed / maxVertical;
            consumption *= 1f + verticalFactor;
        }

        currentFuel -= consumption * Time.deltaTime;
        if (currentFuel <= 0f)
        {
            currentFuel = 0f;
            HandleGameOver("Fuel depleted");
        }
        FuelChanged?.Invoke(currentFuel, maxFuel);
    }

    private void SubscribeToCollisionManager()
    {
        if (subscribedToCollisionManager)
            return;

        if (CollisionManager.Instance == null)
            return;

        CollisionManager.Instance.PackagePickedUp += HandlePackagePickedUp;
        CollisionManager.Instance.PackageDelivered += HandlePackageDelivered;
        CollisionManager.Instance.ObstacleHit += HandleObstacleHit;
        subscribedToCollisionManager = true;
    }

    private void UnsubscribeFromCollisionManager()
    {
        if (!subscribedToCollisionManager)
            return;

        if (CollisionManager.Instance == null)
            return;

        CollisionManager.Instance.PackagePickedUp -= HandlePackagePickedUp;
        CollisionManager.Instance.PackageDelivered -= HandlePackageDelivered;
        CollisionManager.Instance.ObstacleHit -= HandleObstacleHit;
        subscribedToCollisionManager = false;
    }

    private void InitializeLevel(int level)
    {
        currentDeliveries = 0;
        currentFuel = maxFuel;
        isGameOver = false;
        isWin = false;
        int target = GetDeliveryTarget(level);
        Debug.Log($"Initializing level {level} with goal {target}");
        GameStart?.Invoke(target);
        FuelChanged?.Invoke(currentFuel, maxFuel);
    }

    private int GetDeliveryTarget(int level)
    {
        if (levelDeliveryGoals.TryGetValue(level, out int goal))
            return goal;
        return 3;
    }

    private void HandlePackagePickedUp()
    {
        if (isGameOver || isWin)
            return;

        Debug.Log("GameManager: Package picked up");
        StateMachine.Instance?.ChangeState(StateMachine.Instance.GetPackageState());
    }

    private void HandlePackageDelivered()
    {
        if (isGameOver || isWin)
            return;

        currentDeliveries++;
        int target = GetDeliveryTarget(currentLevel);
        Debug.Log($"GameManager: Package delivered ({currentDeliveries}/{target})");
        PackageDelivered?.Invoke(currentDeliveries, target);

        if (currentDeliveries >= target)
        {
            HandleWin();
        }
        else
        {
            StateMachine.Instance?.ChangeState(StateMachine.Instance.GetDeliveredState());
        }
    }

    private void HandleObstacleHit()
    {
        HandleGameOver("Obstacle hit");
    }

    private void HandleGameOver(string reason)
    {
        if (isGameOver)
            return;

        isGameOver = true;
        Debug.Log($"GameManager: Game over ({reason})");
        StateMachine.Instance?.ChangeState(StateMachine.Instance.GetGameOverState());
        GameOver?.Invoke();
    }

    private void HandleWin()
    {
        if (isWin)
            return;

        isWin = true;
        Debug.Log("GameManager: Win condition reached");
        StateMachine.Instance?.ChangeState(StateMachine.Instance.GetWinState());
        Win?.Invoke();
    }
}
