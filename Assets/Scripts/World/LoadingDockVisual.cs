using UnityEngine;

public class LoadingDockVisual : MonoBehaviour
{
    [Header("Dock Slots")]
    [SerializeField] private GameObject[] dockSlots;
    [SerializeField] private bool autoCollectChildren = true;

    private void Awake()
    {
        CacheSlotsIfNeeded();
        SetDockCount(0);
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

    public void SetDockCount(int count)
    {
        CacheSlotsIfNeeded();

        int visibleCount = Mathf.Max(0, count);
        if (dockSlots == null)
            return;

        visibleCount = Mathf.Min(visibleCount, dockSlots.Length);

        for (int i = 0; i < dockSlots.Length; i++)
        {
            GameObject dockSlot = dockSlots[i];
            if (dockSlot == null)
                continue;

            dockSlot.SetActive(i < visibleCount);
        }
    }

    private void SubscribeToGameManager()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.OnGameStart -= HandleGameStart;
        GameManager.Instance.OnGameStart += HandleGameStart;
    }

    private void UnsubscribeFromGameManager()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.OnGameStart -= HandleGameStart;
    }

    private void SyncWithGameManager()
    {
        if (GameManager.Instance != null)
            SetDockCount(GameManager.Instance.RequiredPackages);
    }

    private void HandleGameStart(int requiredPackages)
    {
        SetDockCount(requiredPackages);
    }

    private void CacheSlotsIfNeeded()
    {
        if (!autoCollectChildren || (dockSlots != null && dockSlots.Length > 0))
            return;

        int childCount = transform.childCount;
        dockSlots = new GameObject[childCount];

        for (int i = 0; i < childCount; i++)
            dockSlots[i] = transform.GetChild(i).gameObject;
    }
}
