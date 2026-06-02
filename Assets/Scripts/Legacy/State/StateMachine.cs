using UnityEngine;

// Legacy system: no longer used by active gameplay.
// GameManager.GameState is the only active source of truth for runtime state.
public class StateMachine : MonoBehaviour
{

    public State currentState;
    public HelicopterController character;
    public Animator animator;

    private StartState startState;
    private NoPackageState noPackageState;
    private PackageState packageState;
    private DeliveredState deliveredState;
    private GameOverState gameOverState;
    private WinState winState;
    public static StateMachine Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        character = GetComponent<HelicopterController>();
        animator = GetComponent<Animator>();

        startState = new StartState(this);
        noPackageState = new NoPackageState(this);
        packageState = new PackageState(this);
        deliveredState = new DeliveredState(this);
        gameOverState = new GameOverState(this);
        winState = new WinState(this);
    }

    void Start()
    {
        ChangeState(startState);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        currentState.Update();
    }

    public void ChangeState(State target)
    {
        if (target == null) return;
        currentState?.Exit();
        currentState = target;
        currentState.Enter();
    }

    public StartState GetStartState() => startState;
    public NoPackageState GetNoPackageState() => noPackageState;
    public PackageState GetPackageState() => packageState;
    public DeliveredState GetDeliveredState() => deliveredState;
    public GameOverState GetGameOverState() => gameOverState;
    public WinState GetWinState() => winState;
}
