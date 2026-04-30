using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset;

    [Header("Smoothing")]
    [Tooltip("Lower values follow more tightly, higher values add more delay.")]
    [SerializeField, Min(0f)] private float followDelay = 0.15f;

    private Vector3 velocity;
    private float fixedZ;

    void Awake()
    {
        fixedZ = transform.position.z;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;
        desiredPosition.z = fixedZ;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            followDelay);
    }
}
