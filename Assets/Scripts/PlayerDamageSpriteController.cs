using UnityEngine;

public class PlayerDamageSpriteController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private HelicopterController helicopterController;
    [SerializeField] private SpriteRenderer[] damageStateRenderers;
    [SerializeField] private SpriteRenderer[] facingRightDamageRenderers;
    [SerializeField] private SpriteRenderer[] facingLeftDamageRenderers;
    [SerializeField] private SpriteRenderer[] turningDamageRenderers;

    private int currentDamageTaken;
    private int currentDirection = 1;

    private void OnEnable()
    {
        BindEvents();
    }

    private void Start()
    {
        BindEvents();
    }

    private void OnDisable()
    {
        UnbindEvents();
    }

    private void BindEvents()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();

        if (helicopterController == null)
            helicopterController = FindFirstObjectByType<HelicopterController>();

        if (gameManager == null)
            return;

        gameManager.OnGameStart -= HandleGameStart;
        gameManager.OnGameStart += HandleGameStart;
        gameManager.OnHeliConditionChanged -= HandleHeliConditionChanged;
        gameManager.OnHeliConditionChanged += HandleHeliConditionChanged;

        if (helicopterController != null)
        {
            helicopterController.OnDirectionVisualChanged -= HandleDirectionVisualChanged;
            helicopterController.OnDirectionVisualChanged += HandleDirectionVisualChanged;
            currentDirection = helicopterController.CurrentFacingDirection;
        }

        HandleHeliConditionChanged(gameManager.CurrentHeliCondition, gameManager.MaxHeliCondition);
    }

    private void UnbindEvents()
    {
        if (gameManager == null)
            return;

        gameManager.OnGameStart -= HandleGameStart;
        gameManager.OnHeliConditionChanged -= HandleHeliConditionChanged;

        if (helicopterController != null)
            helicopterController.OnDirectionVisualChanged -= HandleDirectionVisualChanged;
    }

    private void HandleGameStart(int _)
    {
        SetDamageStage(0);
    }

    private void HandleHeliConditionChanged(int current, int max)
    {
        int damageTaken = Mathf.Max(0, max - current);
        SetDamageStage(damageTaken);
    }

    private void HandleDirectionVisualChanged(int direction)
    {
        currentDirection = direction;
        SetDamageStage(currentDamageTaken);
    }

    private void SetDamageStage(int damageTaken)
    {
        currentDamageTaken = damageTaken;

        if (HasDirectionalDamageRenderers())
        {
            SetDamageStage(facingRightDamageRenderers, currentDirection > 0 ? damageTaken : 0);
            SetDamageStage(facingLeftDamageRenderers, currentDirection < 0 ? damageTaken : 0);
            SetDamageStage(turningDamageRenderers, currentDirection == 0 ? damageTaken : 0);
            return;
        }

        SetDamageStage(damageStateRenderers, damageTaken);
    }

    private bool HasDirectionalDamageRenderers()
    {
        return HasAnyRenderer(facingRightDamageRenderers) ||
               HasAnyRenderer(facingLeftDamageRenderers) ||
               HasAnyRenderer(turningDamageRenderers);
    }

    private bool HasAnyRenderer(SpriteRenderer[] renderers)
    {
        return renderers != null && renderers.Length > 0;
    }

    private void SetDamageStage(SpriteRenderer[] renderers, int damageTaken)
    {
        if (renderers == null)
            return;

        int activeIndex = damageTaken - 1;
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer damageRenderer = renderers[i];
            if (damageRenderer == null)
                continue;

            damageRenderer.enabled = i == activeIndex;
        }
    }
}
