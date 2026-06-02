using System;
using UnityEngine;

public class DeliveryGoalVisual : MonoBehaviour
{
    [Header("Dummy Packages")]
    [SerializeField] private GameObject[] dummyPackages;

    public event Action<int, int> OnGoalProgressChanged;

    public int DeliveredPackages { get; private set; }
    public int RequiredPackages { get; private set; }

    private GameManager subscribedGameManager;

    private void Awake()
    {
        SetDummyPackagesVisible(0);
    }

    private void OnEnable()
    {
        SubscribeToGameManager();
        SyncWithGameManager();
    }

    private void Start()
    {
        SubscribeToGameManager();
        SyncWithGameManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromGameManager();
    }

    private void SubscribeToGameManager()
    {
        if (subscribedGameManager != null)
            return;

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
            return;

        subscribedGameManager = gameManager;
        subscribedGameManager.OnGameStart += HandleGameStart;
        subscribedGameManager.OnPackageDelivered += HandlePackageDelivered;
    }

    private void UnsubscribeFromGameManager()
    {
        if (subscribedGameManager == null)
            return;

        subscribedGameManager.OnGameStart -= HandleGameStart;
        subscribedGameManager.OnPackageDelivered -= HandlePackageDelivered;
        subscribedGameManager = null;
    }

    private void SyncWithGameManager()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
            return;

        SetProgress(gameManager.DeliveredPackages, gameManager.RequiredPackages);
    }

    private void HandleGameStart(int requiredPackages)
    {
        SetProgress(0, requiredPackages);
    }

    private void HandlePackageDelivered(int deliveredPackages, int requiredPackages)
    {
        SetProgress(deliveredPackages, requiredPackages);
    }

    private void SetProgress(int deliveredPackages, int requiredPackages)
    {
        DeliveredPackages = Mathf.Max(0, deliveredPackages);
        RequiredPackages = Mathf.Max(0, requiredPackages);

        int visiblePackages = RequiredPackages > 0
            ? Mathf.Min(DeliveredPackages, RequiredPackages)
            : DeliveredPackages;

        visiblePackages = Mathf.Min(visiblePackages, dummyPackages != null ? dummyPackages.Length : 0);
        SetDummyPackagesVisible(visiblePackages);

        OnGoalProgressChanged?.Invoke(DeliveredPackages, RequiredPackages);
    }

    private void SetDummyPackagesVisible(int visibleCount)
    {
        if (dummyPackages == null)
            return;

        for (int i = 0; i < dummyPackages.Length; i++)
        {
            GameObject dummyPackage = dummyPackages[i];
            if (dummyPackage == null)
                continue;

            dummyPackage.SetActive(i < visibleCount);
        }
    }
}
