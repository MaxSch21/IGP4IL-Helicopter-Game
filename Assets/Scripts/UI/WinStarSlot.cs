using UnityEngine;

public class WinStarSlot : MonoBehaviour
{
    [SerializeField] private GameObject emptyStar;
    [SerializeField] private GameObject filledStar;

    private void Awake()
    {
        SetFilled(false);
    }

    private void OnEnable()
    {
        SetFilled(false);
    }

    public void SetFilled(bool filled)
    {
        if (emptyStar != null)
            emptyStar.SetActive(!filled);

        if (filledStar != null)
            filledStar.SetActive(filled);
    }
}
