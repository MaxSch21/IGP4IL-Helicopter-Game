using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CloudLightningToggle : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private GameObject targetObject;
    [SerializeField] private GameObject preTargetObject;
    [SerializeField] private Color preTargetTint = Color.yellow;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float startDelay = 1f;
    [SerializeField, Min(0.01f)] private float lightningDelay = 0.1f;
	[SerializeField, Min(0.01f)] private float preLightningDelay = 0.5f;

    private Vector3 originalScale;
    private SpriteRenderer preTargetSpriteRenderer;
    private Graphic preTargetGraphic;
    private Color originalPreTargetColor = Color.white;
    private bool hasOriginalPreTargetColor;
    private Coroutine loopRoutine;

    void Awake()
    {
        if (targetObject == null)
            return;

        originalScale = targetObject.transform.localScale;

        CachePreTargetColor();

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

        ResetPreTargetColor();
    }

    private IEnumerator LightningLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(startDelay);
            yield return AnimatePreTargetTint(true, preLightningDelay);
            yield return Flash(true, false);
            yield return Flash(false, false);
            yield return Flash(true, true);
            yield return Flash(false, false);
            yield return AnimatePreTargetTint(false, preLightningDelay);
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

    private void CachePreTargetColor()
    {
        if (preTargetObject == null)
            return;

        preTargetSpriteRenderer = preTargetObject.GetComponent<SpriteRenderer>();
        preTargetGraphic = preTargetObject.GetComponent<Graphic>();

        if (preTargetSpriteRenderer != null)
        {
            originalPreTargetColor = preTargetSpriteRenderer.color;
            hasOriginalPreTargetColor = true;
            return;
        }

        if (preTargetGraphic != null)
        {
            originalPreTargetColor = preTargetGraphic.color;
            hasOriginalPreTargetColor = true;
        }
    }

    private void SetPreTargetTint(bool tinted)
    {
        if (preTargetObject == null)
            return;

        if (!hasOriginalPreTargetColor)
            CachePreTargetColor();

        if (preTargetSpriteRenderer != null)
        {
            preTargetSpriteRenderer.color = tinted ? preTargetTint : originalPreTargetColor;
            return;
        }

        if (preTargetGraphic != null)
            preTargetGraphic.color = tinted ? preTargetTint : originalPreTargetColor;
    }

    private IEnumerator AnimatePreTargetTint(bool tinted, float duration)
    {
        if (preTargetObject == null)
            yield break;

        if (duration <= 0f)
        {
            SetPreTargetTint(tinted);
            yield break;
        }

        if (!hasOriginalPreTargetColor)
            CachePreTargetColor();

        Color fromColor = tinted ? originalPreTargetColor : preTargetTint;
        Color toColor = tinted ? preTargetTint : originalPreTargetColor;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetPreTargetColor(Color.Lerp(fromColor, toColor, t));
            yield return null;
        }

        SetPreTargetColor(toColor);
    }

    private void SetPreTargetColor(Color color)
    {
        if (preTargetSpriteRenderer != null)
        {
            preTargetSpriteRenderer.color = color;
            return;
        }

        if (preTargetGraphic != null)
            preTargetGraphic.color = color;
    }

    private void ResetPreTargetColor()
    {
        SetPreTargetTint(false);
    }
}
