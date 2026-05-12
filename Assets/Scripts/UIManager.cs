using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private TMP_Text deliveryText;
    [SerializeField] private TMP_Text temporaryScoreText;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text winFinalScoreText;
    [SerializeField] private TMP_Text winHighScoreText;
    [SerializeField] private TMP_Text newHighScoreMessageText;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Slider fuelSlider;
    [SerializeField, Min(0f)] private float gameOverPanelDelay = 1f;

    private bool subscribedToGameManager;
    private bool subscribedToScoreManager;
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
        SubscribeToScoreManager();
    }

    void Start()
    {
        HidePanels();
        SetDeliveryText("0/0");
        SetTemporaryScoreText(0);
        SetWinScoreTexts(0, 0, false);
        SubscribeToGameManager();
        SubscribeToScoreManager();
    }

    void OnDisable()
    {
        StopGameOverRoutine();
        UnsubscribeFromGameManager();
        UnsubscribeFromScoreManager();
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
        GameManager.Instance.OnHeliConditionChanged += OnHeliConditionChanged;
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
        GameManager.Instance.OnHeliConditionChanged -= OnHeliConditionChanged;
        subscribedToGameManager = false;
    }

    void SubscribeToScoreManager()
    {
        if (subscribedToScoreManager) return;
        if (ScoreManager.Instance == null) return;

        ScoreManager.Instance.OnTemporaryScoreChanged += OnTemporaryScoreChanged;
        ScoreManager.Instance.OnFinalScoreChanged += OnFinalScoreChanged;
        subscribedToScoreManager = true;

        OnTemporaryScoreChanged(ScoreManager.Instance.TemporaryScore);
    }

    void UnsubscribeFromScoreManager()
    {
        if (!subscribedToScoreManager) return;
        if (ScoreManager.Instance == null) return;

        ScoreManager.Instance.OnTemporaryScoreChanged -= OnTemporaryScoreChanged;
        ScoreManager.Instance.OnFinalScoreChanged -= OnFinalScoreChanged;
        subscribedToScoreManager = false;
    }

    public void OnGameStart(int maxPackages)
    {
        Debug.Log($"UIManager: Game started, required packages: {maxPackages}");
        StopGameOverRoutine();
        SetGameplayPaused(false);
        HidePanels();
        SetDeliveryText($"0/{maxPackages}");
        SetTemporaryScoreText(0);
        SetWinScoreTexts(0, 0, false);
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

        if (nextLevelButton != null)
            nextLevelButton.interactable = GameManager.Instance != null && GameManager.Instance.HasNextLevel;
    }

    public void OnNextLevelClicked()
    {
        Debug.Log("UIManager: Next level clicked");
        SetGameplayPaused(false);

        if (GameManager.Instance != null)
            GameManager.Instance.LoadNextLevel();
        else
            Debug.LogWarning("UIManager: No GameManager available for next level load.");
    }

    public void OnHeliConditionChanged(int current, int max)
    {
        Debug.Log($"Heli Condition: {current}/{max}");
    }

    public void OnTemporaryScoreChanged(int score)
    {
        SetTemporaryScoreText(score);
    }

    public void OnFinalScoreChanged(int finalScore, int highScore, bool isNewHighScore)
    {
        SetWinScoreTexts(finalScore, highScore, isNewHighScore);
    }

    private void HidePanels()
    {
        if (winPanel != null)
            winPanel.SetActive(false);
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        if (newHighScoreMessageText != null)
            newHighScoreMessageText.gameObject.SetActive(false);
        if (nextLevelButton != null)
            nextLevelButton.interactable = false;
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

    private void SetTemporaryScoreText(int score)
    {
        if (temporaryScoreText != null)
            temporaryScoreText.text = $"Score: {score}";
    }

    private void SetWinScoreTexts(int finalScore, int highScore, bool isNewHighScore)
    {
        if (winFinalScoreText != null)
            winFinalScoreText.text = $"Final Score: {finalScore}";

        if (winHighScoreText != null)
            winHighScoreText.text = $"Highscore: {highScore}";

        if (newHighScoreMessageText != null)
        {
            newHighScoreMessageText.gameObject.SetActive(isNewHighScore);
            if (isNewHighScore)
                newHighScoreMessageText.text = "New Highscore!";
        }
    }
}
