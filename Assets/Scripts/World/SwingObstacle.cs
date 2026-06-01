using UnityEngine;

public class SwingObstacle : MonoBehaviour
{
    [Header("Pivot")]
    [SerializeField] private Transform pivot;

    [Header("Object")]
    [SerializeField] private Transform swingObject;

    [Header("Swing")]
    [Tooltip("Maximaler Ausschlag in Grad.")]
    [SerializeField, Min(0f)] private float swingAngle = 30f;

    [Tooltip("Dauer fuer einen kompletten Hin-und-zurueck-Zyklus in Sekunden.")]
    [SerializeField, Min(0.01f)] private float swingDuration = 2f;

    [Tooltip("Ebene des Schwingens. Fuer 2D ist das normalerweise Z.")]
    [SerializeField] private Vector3 swingAxis = Vector3.forward;

    private Vector3 initialOffset;
    private Quaternion initialRotation;

    void Awake()
    {
        if (swingObject == null)
            swingObject = transform;

        CacheInitialState();
    }

    void LateUpdate()
    {
        if (pivot == null || swingObject == null || swingDuration <= 0f)
            return;

        if (swingAxis.sqrMagnitude < 0.0001f)
            swingAxis = Vector3.forward;

        float angle = Mathf.Sin(Time.time * Mathf.PI * 2f / swingDuration) * swingAngle;
        Quaternion swingRotation = Quaternion.AngleAxis(angle, swingAxis.normalized);

        swingObject.position = pivot.position + swingRotation * initialOffset;
        swingObject.rotation = swingRotation * initialRotation;
    }

    private void CacheInitialState()
    {
        if (pivot == null)
            return;

        if (swingObject == null)
            swingObject = transform;

        initialOffset = swingObject.position - pivot.position;
        initialRotation = swingObject.rotation;
    }

    void OnValidate()
    {
        if (swingDuration < 0.01f)
            swingDuration = 0.01f;

        if (swingObject == null)
            swingObject = transform;

        if (pivot != null && initialOffset == Vector3.zero)
            CacheInitialState();
    }
}
