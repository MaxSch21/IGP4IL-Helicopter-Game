using System.Collections;
using UnityEngine;

public class CloudLightningToggle : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private GameObject targetObject;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float startDelay = 1f;
    [SerializeField, Min(0.01f)] private float lightningDelay = 0.1f;

    private Vector3 originalScale;
    private Coroutine loopRoutine;

    void Awake()
    {
        if (targetObject == null)
            return;

        originalScale = targetObject.transform.localScale;

        targetObject.SetActive(false);
    }

    void OnEnable()
    {
        if (targetObject == null)
            return;

        loopRoutine = StartCoroutine(LightningLoop());
    }

    void OnDisable()
    {
        if (loopRoutine != null)
        {
            StopCoroutine(loopRoutine);
            loopRoutine = null;
        }
    }

    private IEnumerator LightningLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(startDelay);
            yield return Flash(true, false);
            yield return Flash(false, false);
            yield return Flash(true, true);
            yield return Flash(false, false);
        }
    }

    private IEnumerator Flash(bool active, bool mirrored)
    {
        SetTargetState(active, mirrored);
        yield return new WaitForSeconds(lightningDelay);
    }

    private void SetTargetState(bool active, bool mirrored)
    {
        if (targetObject == null)
            return;

        if (active)
        {
            Vector3 scale = originalScale;
            if (mirrored)
                scale.x *= -1f;

            targetObject.transform.localScale = scale;
        }
        else
        {
            targetObject.transform.localScale = originalScale;
        }

        targetObject.SetActive(active);
    }
}
