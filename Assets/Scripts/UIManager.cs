using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private TMP_Text deliveryText;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Slider fuelSlider;
    [SerializeField, Min(0f)] private float gameOverPanelDelay = 1f;

    private bool subscribedToGameManager;
    private Coroutine gameOverRoutine;

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
        StopGameOverRoutine();
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
        StopGameOverRoutine();
        SetGameplayPaused(false);
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
        StartGameOverRoutine();
    }

    public void OnWin()
    {
        Debug.Log("UIManager: Win");
        StopGameOverRoutine();
        SetGameplayPaused(false);
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

    private void StartGameOverRoutine()
    {
        StopGameOverRoutine();
        gameOverRoutine = StartCoroutine(ShowGameOverPanelAfterDelay());
    }

    private void StopGameOverRoutine()
    {
        if (gameOverRoutine == null)
            return;

        StopCoroutine(gameOverRoutine);
        gameOverRoutine = null;
    }

    private IEnumerator ShowGameOverPanelAfterDelay()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (gameOverPanelDelay > 0f)
            yield return new WaitForSecondsRealtime(gameOverPanelDelay);

        SetGameplayPaused(true);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        gameOverRoutine = null;
    }

    private void SetGameplayPaused(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;
    }

    private void SetDeliveryText(string value)
    {
        if (deliveryText != null)
            deliveryText.text = value;
    }
}
