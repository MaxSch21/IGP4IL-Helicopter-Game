using UnityEngine;

public class DeliveryStationVisual : MonoBehaviour
{
    [Header("Required Package Slots")]
    [SerializeField] private GameObject[] slotObjects;

    [Header("Filled Package Visuals")]
    [SerializeField] private GameObject[] filledPackageVisuals;

    private GameManager subscribedGameManager;

    private void Awake()
    {
        ApplyVisuals(0, 0);
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

        ApplyVisuals(gameManager.DeliveredPackages, gameManager.RequiredPackages);
    }

    private void HandleGameStart(int requiredPackages)
    {
        ApplyVisuals(0, requiredPackages);
    }

    private void HandlePackageDelivered(int deliveredPackages, int requiredPackages)
    {
        ApplyVisuals(deliveredPackages, requiredPackages);
    }

    private void ApplyVisuals(int deliveredPackages, int requiredPackages)
    {
        int safeRequiredPackages = Mathf.Max(0, requiredPackages);
        int safeDeliveredPackages = Mathf.Clamp(deliveredPackages, 0, safeRequiredPackages);

        SetObjectsVisible(slotObjects, safeRequiredPackages);

        GameObject[] fillTargets = HasAssignedEntries(filledPackageVisuals)
            ? filledPackageVisuals
            : slotObjects;

        SetObjectsVisible(fillTargets, safeDeliveredPackages);
    }

    private void SetObjectsVisible(GameObject[] targets, int visibleCount)
    {
        if (targets == null)
            return;

        int safeVisibleCount = Mathf.Max(0, visibleCount);
        for (int i = 0; i < targets.Length; i++)
        {
            GameObject target = targets[i];
            if (target == null)
                continue;

            target.SetActive(i < safeVisibleCount);
        }
    }

    private bool HasAssignedEntries(GameObject[] targets)
    {
        if (targets == null || targets.Length == 0)
            return false;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
                return true;
        }

        return false;
    }
}
