using UnityEngine;

// Legacy menu system: not used by the active project flow.
// MainMenu handles main-menu scene UI, UIManager handles win/game-over HUD, and PauseMenu handles pause UI.
public enum MenuState
{
    Main,
    LevelSelect,
    Pause,
    Win,
    GameOver
}

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject levelSelectPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Initial State")]
    [SerializeField] private MenuState initialState = MenuState.Main;

    public MenuState CurrentState { get; private set; }

    void OnEnable()
    {
        MenuEvents.OnMenuStateRequested += SetState;
    }

    void OnDisable()
    {
        MenuEvents.OnMenuStateRequested -= SetState;
    }

    void Start()
    {
        SetState(initialState);
    }

    public void SetState(MenuState state)
    {
        CurrentState = state;
        MenuEvents.SyncMenuState(state);

        SetPanelActive(mainMenuPanel, state == MenuState.Main);
        SetPanelActive(levelSelectPanel, state == MenuState.LevelSelect);
        SetPanelActive(pausePanel, state == MenuState.Pause);
        SetPanelActive(winPanel, state == MenuState.Win);
        SetPanelActive(gameOverPanel, state == MenuState.GameOver);
    }

    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
}
