using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset;

    [Header("Bounds")]
    [SerializeField] private bool constrainToArea = true;
    [SerializeField] private Vector2 areaCenter;
    [SerializeField, Min(0f)] private Vector2 areaSize = new Vector2(20f, 10f);

    [Header("Smoothing")]
    [Tooltip("Lower values follow more tightly, higher values add more delay.")]
    [SerializeField, Min(0f)] private float followDelay = 0.15f;

    [Header("Gizmo")]
    [SerializeField] private Color boundsColor = new Color(0f, 1f, 0f, 0.9f);

    private Vector3 velocity;
    private float fixedZ;
    private Camera cachedCamera;

    void Awake()
    {
        fixedZ = transform.position.z;
        cachedCamera = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;
        desiredPosition.z = fixedZ;
        desiredPosition = ClampToBounds(desiredPosition);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            followDelay);

        Vector3 clampedPosition = ClampToBounds(transform.position);
        if (clampedPosition.x != transform.position.x)
            velocity.x = 0f;
        if (clampedPosition.y != transform.position.y)
            velocity.y = 0f;

        transform.position = clampedPosition;
    }

    private Vector3 ClampToBounds(Vector3 position)
    {
        if (!constrainToArea)
            return position;

        Camera cam = cachedCamera != null ? cachedCamera : GetComponent<Camera>();
        if (cam == null || !cam.orthographic)
            return position;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        Vector2 halfArea = areaSize * 0.5f;
        float minX = areaCenter.x - halfArea.x + halfWidth;
        float maxX = areaCenter.x + halfArea.x - halfWidth;
        float minY = areaCenter.y - halfArea.y + halfHeight;
        float maxY = areaCenter.y + halfArea.y - halfHeight;

        if (minX > maxX)
            position.x = areaCenter.x;
        else
            position.x = Mathf.Clamp(position.x, minX, maxX);

        if (minY > maxY)
            position.y = areaCenter.y;
        else
            position.y = Mathf.Clamp(position.y, minY, maxY);

        return position;
    }

    void OnDrawGizmos()
    {
        if (!constrainToArea)
            return;

        Gizmos.color = boundsColor;
        Gizmos.DrawWireCube((Vector3)areaCenter, new Vector3(areaSize.x, areaSize.y, 0f));
    }
}
