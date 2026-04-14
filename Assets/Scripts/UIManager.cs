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

        GameManager.Instance.PackageDelivered += OnPackageDelivered;
        GameManager.Instance.GameOver += OnGameOver;
        GameManager.Instance.Win += OnWin;
        GameManager.Instance.GameStart += OnGameStart;
        GameManager.Instance.FuelChanged += OnFuelChanged;
        subscribedToGameManager = true;
        GameManager.Instance.NotifyUIState();
    }

    void UnsubscribeFromGameManager()
    {
        if (!subscribedToGameManager) return;
        if (GameManager.Instance == null) return;

        GameManager.Instance.PackageDelivered -= OnPackageDelivered;
        GameManager.Instance.GameOver -= OnGameOver;
        GameManager.Instance.Win -= OnWin;
        GameManager.Instance.GameStart -= OnGameStart;
        GameManager.Instance.FuelChanged -= OnFuelChanged;
        subscribedToGameManager = false;
    }

    public void OnGameStart(int maxPackages)
    {
        Debug.Log($"UIManager received GameStart(max={maxPackages})");
        HidePanels();
        SetDeliveryText($"0/{maxPackages}");
    }

    public void OnPackageDelivered(int current, int max)
    {
        Debug.Log($"UIManager received PackageDelivered ({current}/{max})");
        SetDeliveryText($"{current}/{max}");
    }

    public void OnFuelChanged(float current, float max)
    {
        Debug.Log($"UIManager received FuelChanged ({current}/{max})");
        if (fuelSlider != null)
        {
            fuelSlider.maxValue = max;
            fuelSlider.value = current;
        }
    }

    public void OnGameOver()
    {
        Debug.Log("UIManager received GameOver");
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    public void OnWin()
    {
        Debug.Log("UIManager received Win");
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
