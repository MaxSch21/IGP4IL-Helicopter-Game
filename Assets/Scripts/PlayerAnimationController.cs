using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Animator animator;
    [SerializeField] private string deathTriggerName = "Death";

    private bool deathTriggered;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        BindEvents();
    }

    void Start()
    {
        BindEvents();
    }

    void OnDisable()
    {
        UnbindEvents();
    }

    private void BindEvents()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();

        if (gameManager == null)
            return;

        gameManager.OnStateChanged -= HandleStateChanged;
        gameManager.OnStateChanged += HandleStateChanged;

        if (gameManager.CurrentState == GameManager.GameState.GameOver)
            HandleStateChanged(GameManager.GameState.GameOver);
    }

    private void UnbindEvents()
    {
        if (gameManager != null)
            gameManager.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameManager.GameState state)
    {
        if (deathTriggered || state != GameManager.GameState.GameOver)
            return;

        deathTriggered = true;

        if (animator == null)
            return;

        if (HasTriggerParameter(deathTriggerName))
        {
            animator.SetTrigger(deathTriggerName);
            return;
        }

    }

    private bool HasTriggerParameter(string parameterName)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == parameterName)
                return true;
        }

        return false;
    }
}
