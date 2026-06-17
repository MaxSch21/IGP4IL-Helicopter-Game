using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class UIButtonSound : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private bool includeInactiveButtons = true;

    private readonly System.Collections.Generic.List<Button> boundButtons = new System.Collections.Generic.List<Button>();

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        BindButtons();
    }

    private void BindButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(includeInactiveButtons);
        foreach (Button button in buttons)
        {
            if (boundButtons.Contains(button))
                continue;

            button.onClick.AddListener(PlayClick);
            boundButtons.Add(button);
        }
    }

    private void OnDestroy()
    {
        foreach (Button button in boundButtons)
        {
            if (button != null)
                button.onClick.RemoveListener(PlayClick);
        }

        boundButtons.Clear();
    }

    private void PlayClick()
    {
        if (audioSource != null && clickClip != null)
            audioSource.PlayOneShot(clickClip);
    }
}
