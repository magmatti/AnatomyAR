using TMPro;
using UnityEngine;

public sealed class BoneLabelUIManager : MonoBehaviour
{
    [SerializeField] private GameObject labelPanelRoot;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private float visibleSeconds = 2.5f;

    private string currentMessage = string.Empty;
    private float visibleUntil;

    public void ShowLabel(string message)
    {
        currentMessage = string.IsNullOrWhiteSpace(message) ? "(empty)" : message;
        visibleUntil = Time.unscaledTime + visibleSeconds;

        if (labelText != null)
        {
            labelText.text = currentMessage;
        }

        if (labelPanelRoot != null)
        {
            labelPanelRoot.SetActive(true);
        }
    }

    public void HideLabel()
    {
        currentMessage = string.Empty;

        if (labelText != null)
        {
            labelText.text = string.Empty;
        }

        if (labelPanelRoot != null)
        {
            labelPanelRoot.SetActive(false);
        }
    }

    public bool HideLabelIfTimeExpired()
    {
        if (!string.IsNullOrEmpty(currentMessage) && Time.unscaledTime > visibleUntil)
        {
            HideLabel();
            return true;
        }

        return false;
    }
}
