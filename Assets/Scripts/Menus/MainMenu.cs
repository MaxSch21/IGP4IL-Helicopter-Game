using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject levelSelectPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button levelSelectButton;
    [SerializeField] private Button levelSelectBackButton;
    [SerializeField] private List<Button> levelButtons = new List<Button>();

    [Header("Levels")]
    [SerializeField] private List<LevelData> levels = new List<LevelData>();

    void Awake()
    {
        BindStaticButtons();
        BindLevelButtons();
    }

    void Start()
    {
        if (EventSystem.current == null)
            Debug.LogWarning("MainMenu: No EventSystem found in this scene. UI clicks will not work.");

        ShowMainMenu();
        RefreshLevelButtons();
    }

    void OnEnable()
    {
        RefreshLevelButtons();
    }

    public void OnPlayClicked()
    {
        Debug.Log("MainMenu: Play clicked");
        LoadFirstPlayableLevel();
    }

    public void OnLevelSelectionClicked()
    {
        Debug.Log("MainMenu: Level selection clicked");
        ShowLevelSelect();
    }

    public void OnBackClicked()
    {
        Debug.Log("MainMenu: Back clicked");
        ShowMainMenu();
    }

    public void OnSettingsClicked()
    {
        Debug.Log("MainMenu: Settings clicked");
        ShowSettings();
    }

    public void OnSettingsBackClicked()
    {
        Debug.Log("MainMenu: Settings back clicked");
        ShowMainMenu();
    }

    public void OnQuitClicked()
    {
        Debug.Log("MainMenu: Quit clicked");
        Application.Quit();
    }

    public void OnLevelClicked(int levelIndex)
    {
        Debug.Log($"MainMenu: Level clicked {levelIndex}");
        LevelData level = levels.Find(candidate => candidate != null && candidate.levelIndex == levelIndex);
        LoadLevel(level);
    }

    public void OnLevelClicked(LevelData level)
    {
        Debug.Log($"MainMenu: Level clicked {level?.levelIndex ?? 0}");
        LoadLevel(level);
    }

    public void ResetProgress()
    {
        LevelProgress.ResetAll(GetHighestConfiguredLevelIndex());
        RefreshLevelButtons();
    }

    private void BindStaticButtons()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayClicked);
        }

        if (levelSelectButton != null)
        {
            levelSelectButton.onClick.RemoveAllListeners();
            levelSelectButton.onClick.AddListener(OnLevelSelectionClicked);
        }

        if (levelSelectBackButton != null)
        {
            levelSelectBackButton.onClick.RemoveAllListeners();
            levelSelectBackButton.onClick.AddListener(OnBackClicked);
        }
    }

    private void BindLevelButtons()
    {
        int count = Mathf.Min(levelButtons.Count, levels.Count);
        for (int i = 0; i < count; i++)
        {
            Button button = levelButtons[i];
            LevelData level = levels[i];

            if (button == null || level == null)
                continue;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => LoadLevel(level));
            SetButtonLabel(button, level);
        }
    }

    private void RefreshLevelButtons()
    {
        if (playButton != null)
            playButton.interactable = HasPlayableLevel();

        if (levelSelectButton != null)
            levelSelectButton.interactable = HasPlayableLevel();

        for (int i = 0; i < levelButtons.Count; i++)
        {
            Button button = levelButtons[i];
            if (button == null)
                continue;

            bool hasLevel = i < levels.Count && levels[i] != null && levels[i].IsConfigured;
            bool unlocked = hasLevel && LevelProgress.IsUnlocked(levels[i].levelIndex);

            button.interactable = unlocked;

            if (hasLevel)
                SetButtonLabel(button, levels[i]);
        }
    }

    private void ShowMainMenu()
    {
        SetPanelActive(mainPanel, true);
        SetPanelActive(settingsPanel, false);
        SetPanelActive(levelSelectPanel, false);
    }

    private void ShowLevelSelect()
    {
        RefreshLevelButtons();
        SetPanelActive(mainPanel, false);
        SetPanelActive(levelSelectPanel, true);
        SetPanelActive(settingsPanel, false);
    }

    private void ShowSettings()
    {
        SetPanelActive(mainPanel, false);
        SetPanelActive(levelSelectPanel, false);
        SetPanelActive(settingsPanel, true);
    }

    private void LoadFirstPlayableLevel()
    {
        LevelData fallbackLevel = null;

        foreach (LevelData level in levels)
        {
            if (level == null || !level.IsConfigured || !LevelProgress.IsUnlocked(level.levelIndex))
                continue;

            fallbackLevel = level;

            if (!LevelProgress.IsCompleted(level.levelIndex))
            {
                LoadLevel(level);
                return;
            }
        }

        if (fallbackLevel != null)
        {
            LoadLevel(fallbackLevel);
            return;
        }

        Debug.LogWarning("MainMenu: No playable level configured.");
    }

    private void LoadLevel(LevelData level)
    {
        if (level == null || !level.IsConfigured)
        {
            Debug.LogWarning("MainMenu: Level is not configured.");
            return;
        }

        if (!LevelProgress.IsUnlocked(level.levelIndex))
        {
            Debug.LogWarning($"MainMenu: Level {level.levelIndex} is locked.");
            return;
        }

        Time.timeScale = 1f;
        LevelRuntime.Set(level);
        Debug.Log($"MainMenu: Loading {level.displayName} ({level.sceneName})");
        SceneManager.LoadScene(level.sceneName);
    }

    private bool HasPlayableLevel()
    {
        foreach (LevelData level in levels)
        {
            if (level != null && level.IsConfigured && LevelProgress.IsUnlocked(level.levelIndex))
                return true;
        }

        return false;
    }

    private int GetHighestConfiguredLevelIndex()
    {
        int highest = 1;
        foreach (LevelData level in levels)
        {
            if (level != null)
                highest = Mathf.Max(highest, level.levelIndex);
        }

        return highest;
    }

    private void SetButtonLabel(Button button, LevelData level)
    {
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label == null)
            return;

        string suffix = LevelProgress.IsCompleted(level.levelIndex) ? " - Done" : "";
        label.text = $"{level.displayName}{suffix}";
    }

    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
}
