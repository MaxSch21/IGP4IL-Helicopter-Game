using UnityEngine;
using UnityEngine.UI;

public class GoalDirectionIndicator : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform player;
    [SerializeField] private string autoTargetTag = "DropZone";

    [Header("Scene References")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform indicator;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Graphic indicatorGraphic;

    [Header("Tuning")]
    [SerializeField] private float edgePadding = 48f;
    [SerializeField] private float visibleTargetOffset = 60f;
    [SerializeField] private float rotationOffsetDegrees = -90f;
    [SerializeField] private bool autoFindTarget = true;

    void Awake()
    {
        ResolveReferences();
    }

    void LateUpdate()
    {
        ResolveReferences();

        if (indicator == null || canvas == null || worldCamera == null)
            return;

        if (target == null)
        {
            SetVisible(false);
            return;
        }

        UpdateIndicator();
        SetVisible(true);
    }

    private void UpdateIndicator()
    {
        Vector3 targetScreenPoint = worldCamera.WorldToScreenPoint(target.position);
        Vector3 referenceScreenPoint = GetReferenceScreenPoint();

        Vector2 direction = (Vector2)(targetScreenPoint - referenceScreenPoint);
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.up;

        if (IsOnScreen(targetScreenPoint))
        {
            Vector2 offset = direction.normalized * visibleTargetOffset;
            targetScreenPoint += new Vector3(offset.x, offset.y, 0f);
        }

        targetScreenPoint = ClampToScreen(targetScreenPoint);
        indicator.anchoredPosition = ScreenToAnchoredPosition((Vector2)targetScreenPoint);
        indicator.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + rotationOffsetDegrees);
    }

    private Vector3 GetReferenceScreenPoint()
    {
        if (player != null)
            return worldCamera.WorldToScreenPoint(player.position);

        return new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
    }

    private bool IsOnScreen(Vector3 screenPoint)
    {
        return screenPoint.x >= 0f &&
               screenPoint.x <= Screen.width &&
               screenPoint.y >= 0f &&
               screenPoint.y <= Screen.height &&
               screenPoint.z > 0f;
    }

    private Vector3 ClampToScreen(Vector3 screenPoint)
    {
        return new Vector3(
            Mathf.Clamp(screenPoint.x, edgePadding, Screen.width - edgePadding),
            Mathf.Clamp(screenPoint.y, edgePadding, Screen.height - edgePadding),
            screenPoint.z);
    }

    private Vector2 ScreenToAnchoredPosition(Vector2 screenPoint)
    {
        RectTransform canvasRect = canvas.transform as RectTransform;
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out Vector2 localPoint))
            return localPoint;

        return Vector2.zero;
    }

    private void ResolveReferences()
    {
        if (indicator == null)
            indicator = transform as RectTransform;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (indicatorGraphic == null && indicator != null)
            indicatorGraphic = indicator.GetComponent<Graphic>();

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (player == null)
        {
            HelicopterController helicopter = FindFirstObjectByType<HelicopterController>();
            if (helicopter != null)
                player = helicopter.transform;
        }

        if (target == null && autoFindTarget && !string.IsNullOrWhiteSpace(autoTargetTag))
        {
            GameObject targetObject = GameObject.FindWithTag(autoTargetTag);
            if (targetObject != null)
                target = targetObject.transform;
        }
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
            return;
        }

        if (indicatorGraphic != null)
            indicatorGraphic.enabled = visible;
    }
}
