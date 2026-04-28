using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private TMP_Text deliveryText;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Slider fuelSlider;
    private bool subscribedToGameManager;

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
        SubscribeToGameManager();
    }

    void Start()
    {
        HidePanels();
        SetDeliveryText("0/0");
        SubscribeToGameManager();
    }

    void OnDisable()
    {
        UnsubscribeFromGameManager();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void SubscribeToGameManager()
    {
        if (subscribedToGameManager) return;
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnPackageDelivered += OnPackageDelivered;
        GameManager.Instance.OnGameOver += OnGameOver;
        GameManager.Instance.OnWin += OnWin;
        GameManager.Instance.OnGameStart += OnGameStart;
        GameManager.Instance.OnFuelChanged += OnFuelChanged;
        subscribedToGameManager = true;
        GameManager.Instance.NotifyUIState();
    }

    void UnsubscribeFromGameManager()
    {
        if (!subscribedToGameManager) return;
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnPackageDelivered -= OnPackageDelivered;
        GameManager.Instance.OnGameOver -= OnGameOver;
        GameManager.Instance.OnWin -= OnWin;
        GameManager.Instance.OnGameStart -= OnGameStart;
        GameManager.Instance.OnFuelChanged -= OnFuelChanged;
        subscribedToGameManager = false;
    }

    public void OnGameStart(int maxPackages)
    {
        Debug.Log($"UIManager: Game started, required packages: {maxPackages}");
        HidePanels();
        SetDeliveryText($"0/{maxPackages}");
    }

    public void OnPackageDelivered(int current, int max)
    {
        Debug.Log($"UIManager: Package delivered ({current}/{max})");
        SetDeliveryText($"{current}/{max}");
    }

    public void OnFuelChanged(float current, float max)
    {
        if (fuelSlider != null)
        {
            fuelSlider.maxValue = max;
            fuelSlider.value = current;
        }
    }

    public void OnGameOver()
    {
        Debug.Log("UIManager: Game over");
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    public void OnWin()
    {
        Debug.Log("UIManager: Win");
        if (winPanel != null)
            winPanel.SetActive(true);
    }

    private void HidePanels()
    {
        if (winPanel != null)
            winPanel.SetActive(false);
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void SetDeliveryText(string value)
    {
        if (deliveryText != null)
            deliveryText.text = value;
    }
}
