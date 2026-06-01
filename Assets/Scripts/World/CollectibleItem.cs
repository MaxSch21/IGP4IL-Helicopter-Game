using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public abstract class CollectibleItem : MonoBehaviour
{
    [Header("Hover")]
    [SerializeField] private bool enableHover = true;
    [SerializeField, Min(0f)] private float hoverAmplitude = 0.15f;
    [SerializeField, Min(0.01f)] private float hoverSpeed = 2f;

    [SerializeField] private bool deactivateOnCollect = true;

    private bool collected;
    private Vector3 startPosition;

    void Reset()
    {
        Collider2D collider2D = GetComponent<Collider2D>();
        collider2D.isTrigger = true;
    }

    void OnEnable()
    {
        collected = false;
        startPosition = transform.position;
    }

    void Update()
    {
        if (!enableHover || collected)
            return;

        float offsetY = Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;
        transform.position = startPosition + new Vector3(0f, offsetY, 0f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || other == null)
            return;

        if (other.GetComponentInParent<HelicopterController>() == null)
            return;

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
            return;

        if (!CanCollect(gameManager))
            return;

        collected = true;
        Collect(gameManager);

        if (deactivateOnCollect)
            gameObject.SetActive(false);
    }

    protected virtual void OnDisable()
    {
        transform.position = startPosition;
    }

    protected virtual bool CanCollect(GameManager gameManager)
    {
        return true;
    }

    protected abstract void Collect(GameManager gameManager);
}
