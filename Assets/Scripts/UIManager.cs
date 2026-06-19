using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    private const int StarBonusPoints = 100;

    public static UIManager Instance { get; private set; }

    [SerializeField] private TMP_Text deliveryText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text temporaryScoreText;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text winFinalScoreText;
    [SerializeField] private TMP_Text winHighScoreText;
    [SerializeField] private TMP_Text winTimeText;
    [SerializeField] private TMP_Text newHighScoreMessageText;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Slider fuelSlider;
    [SerializeField] private GameObject lowFuelWarningObject;
    [SerializeField] private WinStarSlot[] winStarSlots;
    [SerializeField, Min(0f)] private float gameOverPanelDelay = 1f;
    [SerializeField, Min(0f)] private float winStarRevealDelay = 0.25f;
    [SerializeField, Range(0f, 1f)] private float lowFuelWarningThreshold = 0.2f;
    [SerializeField, Min(0.01f)] private float lowFuelBlinkInterval = 0.35f;

    private bool subscribedToGameManager;
    private bool subscribedToScoreManager;
    private Coroutine gameOverRoutine;
    private Coroutine winStarRoutine;
    private Coroutine lowFuelWarningRoutine;

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
        SetDeliveryText("");
        SetTimeTexts(0f);
        ResetWinStars();
        SubscribeToGameManager();
        SubscribeToScoreManager();
    }

    void Update()
    {
        if (GameManager.Instance == null)
            return;

        SetTimeTexts(GameManager.Instance.ElapsedTime);
    }

    void OnDisable()
    {
        StopGameOverRoutine();
        StopWinStarRoutine();
        StopLowFuelWarning();
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
        OnFinalScoreChanged(ScoreManager.Instance.FinalScore, ScoreManager.Instance.HighScore, ScoreManager.Instance.IsNewHighScore);
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
        StopGameOverRoutine();
        StopWinStarRoutine();
        SetGameplayPaused(false);
        HidePanels();
        SetDeliveryText("");
        SetTimeTexts(0f);
        ResetWinStars();
    }

    public void OnPackageDelivered(int current, int max)
    {
    }

    public void OnFuelChanged(float current, float max)
    {
        if (fuelSlider != null)
        {
            fuelSlider.maxValue = max;
            fuelSlider.value = current;
        }

        UpdateLowFuelWarning(current, max);
    }

    public void OnGameOver()
    {
        StartGameOverRoutine();
    }

    public void OnWin()
    {
        StopGameOverRoutine();
        StopWinStarRoutine();
        SetGameplayPaused(false);
        if (winPanel != null)
            winPanel.SetActive(true);

        StartWinStarRoutine();

        if (nextLevelButton != null)
            nextLevelButton.interactable = GameManager.Instance != null && GameManager.Instance.HasNextLevel;
    }

    public void OnNextLevelClicked()
    {
        SetGameplayPaused(false);

        if (GameManager.Instance != null)
            GameManager.Instance.LoadNextLevel();
    }

    public void OnHeliConditionChanged(int current, int max)
    {
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

    private void StartWinStarRoutine()
    {
        winStarRoutine = StartCoroutine(AnimateWinStars());
    }

    private void StopWinStarRoutine()
    {
        if (winStarRoutine == null)
            return;

        StopCoroutine(winStarRoutine);
        winStarRoutine = null;
    }

    private void UpdateLowFuelWarning(float currentFuel, float maxFuel)
    {
        if (lowFuelWarningObject == null || maxFuel <= 0f)
            return;

        bool shouldWarn = currentFuel > 0f && currentFuel <= maxFuel * lowFuelWarningThreshold;

        if (shouldWarn)
        {
            StartLowFuelWarning();
            return;
        }

        StopLowFuelWarning();
    }

    private void StartLowFuelWarning()
    {
        if (lowFuelWarningRoutine != null || lowFuelWarningObject == null)
            return;

        lowFuelWarningRoutine = StartCoroutine(BlinkLowFuelWarning());
    }

    private void StopLowFuelWarning()
    {
        if (lowFuelWarningRoutine != null)
        {
            StopCoroutine(lowFuelWarningRoutine);
            lowFuelWarningRoutine = null;
        }

        if (lowFuelWarningObject != null)
            lowFuelWarningObject.SetActive(false);
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

    private void SetTimeTexts(float elapsedSeconds)
    {
        string formattedTime = $"Time: {FormatTime(elapsedSeconds)}";

        if (timeText != null)
            timeText.text = formattedTime;

        if (winTimeText != null)
            winTimeText.text = formattedTime;
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

    private IEnumerator AnimateWinStars()
    {
        int filledStars = 0;
        int targetFinalScore = 0;
        int highScore = 0;
        bool isNewHighScore = false;

        if (GameManager.Instance != null)
            filledStars = StarEvaluator.Evaluate(GameManager.Instance.LastLevelResult);

        if (ScoreManager.Instance != null)
        {
            targetFinalScore = ScoreManager.Instance.FinalScore;
            highScore = ScoreManager.Instance.HighScore;
            isNewHighScore = ScoreManager.Instance.IsNewHighScore;
        }

        int baseFinalScore = Mathf.Max(0, targetFinalScore - filledStars * StarBonusPoints);

        SetWinStars(0);
        SetWinScoreTexts(baseFinalScore, highScore, isNewHighScore);

        for (int i = 0; i < filledStars; i++)
        {
            SetWinStars(i + 1);
            SetWinScoreTexts(baseFinalScore + (i + 1) * StarBonusPoints, highScore, isNewHighScore);

            if (winStarRevealDelay > 0f && i < filledStars - 1)
                yield return new WaitForSecondsRealtime(winStarRevealDelay);
        }

        SetWinScoreTexts(targetFinalScore, highScore, isNewHighScore);
        winStarRoutine = null;
    }

    private IEnumerator BlinkLowFuelWarning()
    {
        while (true)
        {
            lowFuelWarningObject.SetActive(true);
            yield return new WaitForSeconds(lowFuelBlinkInterval);

            lowFuelWarningObject.SetActive(false);
            yield return new WaitForSeconds(lowFuelBlinkInterval);
        }
    }

    private void ResetWinStars()
    {
        SetWinStars(0);
    }

    private void SetWinStars(int filledStars)
    {
        if (winStarSlots == null)
            return;

        for (int i = 0; i < winStarSlots.Length; i++)
        {
            WinStarSlot slot = winStarSlots[i];
            if (slot == null)
                continue;

            slot.SetFilled(i < filledStars);
        }
    }

    private string FormatTime(float elapsedSeconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(elapsedSeconds));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:00}:{seconds:00}";
    }
}
