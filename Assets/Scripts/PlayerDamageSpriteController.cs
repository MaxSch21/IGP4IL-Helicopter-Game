using UnityEngine;

public class PlayerDamageSpriteController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private SpriteRenderer[] damageStateRenderers;

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

        if (gameManager == null)
            return;

        gameManager.OnGameStart -= HandleGameStart;
        gameManager.OnGameStart += HandleGameStart;
        gameManager.OnHeliConditionChanged -= HandleHeliConditionChanged;
        gameManager.OnHeliConditionChanged += HandleHeliConditionChanged;

        HandleHeliConditionChanged(gameManager.CurrentHeliCondition, gameManager.MaxHeliCondition);
    }

    private void UnbindEvents()
    {
        if (gameManager == null)
            return;

        gameManager.OnGameStart -= HandleGameStart;
        gameManager.OnHeliConditionChanged -= HandleHeliConditionChanged;
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

    private void SetDamageStage(int damageTaken)
    {
        if (damageStateRenderers == null)
            return;

        int activeIndex = damageTaken - 1;
        for (int i = 0; i < damageStateRenderers.Length; i++)
        {
            SpriteRenderer damageRenderer = damageStateRenderers[i];
            if (damageRenderer == null)
                continue;

            damageRenderer.enabled = i == activeIndex;
        }
    }
}
