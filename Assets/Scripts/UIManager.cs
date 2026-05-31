using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private GameObject instructionsPanel;

    public void OnHandButtonPressed()
    {
        Debug.Log("Hand pressed!");
        SceneManager.LoadScene("HandTrackingScene");
    }

    public void OnSkeletonButtonPressed()
    {
        Debug.Log("Skeleton pressed!");
        SceneManager.LoadScene("WholeBodyTrackingScene");
    }

    public void OnInstructionsButtonPressed()
    {
        Debug.Log("Instructions pressed!");
        if (instructionsPanel == null)
        {
            CreateInstructionsPanel();
        }

        if (instructionsPanel != null)
        {
            instructionsPanel.SetActive(true);
        }
    }

    public void OnBackButtonPressed()
    {
        Debug.Log("Back pressed!");
        SceneManager.LoadScene("MainMenuScene");
    }

    public void OnQuitButtonPressed()
    {
        Debug.Log("Quit pressed!");
        Application.Quit();
    }

    private void CreateInstructionsPanel()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            Debug.LogWarning("Instructions panel could not be created because no Canvas was found.");
            return;
        }

        instructionsPanel = new GameObject("InstructionsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        instructionsPanel.transform.SetParent(canvas.transform, false);

        RectTransform overlayRect = instructionsPanel.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = instructionsPanel.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.72f);

        GameObject dialog = new GameObject("InstructionsDialog", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dialog.transform.SetParent(instructionsPanel.transform, false);

        RectTransform dialogRect = dialog.GetComponent<RectTransform>();
        dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
        dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRect.pivot = new Vector2(0.5f, 0.5f);
        dialogRect.anchoredPosition = Vector2.zero;
        dialogRect.sizeDelta = new Vector2(330f, 500f);

        Image dialogImage = dialog.GetComponent<Image>();
        dialogImage.color = new Color(0.08f, 0.08f, 0.08f, 0.96f);

        TextMeshProUGUI title = CreateText("InstructionsTitle", dialog.transform, "How to use Anatomy AR", 24f, FontStyles.Bold);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -34f);
        titleRect.sizeDelta = new Vector2(-36f, 42f);
        title.alignment = TextAlignmentOptions.Center;

        TextMeshProUGUI body = CreateText(
            "InstructionsBody",
            dialog.transform,
            "Hand tracking\nPoint the camera at your hand and keep it visible. The app displays a hand model over your hand. Tap bones to show name annotations.\n\nSkeleton tracking\nPoint the camera at a real person and keep their body in view. The app displays the skeleton model. Tap bones to show name annotations.",
            16f,
            FontStyles.Normal
        );

        RectTransform bodyRect = body.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(24f, 86f);
        bodyRect.offsetMax = new Vector2(-24f, -82f);
        body.alignment = TextAlignmentOptions.TopLeft;
        body.enableWordWrapping = true;

        Button dismissButton = CreateButton("DismissButton", dialog.transform, "Dismiss");
        RectTransform buttonRect = dismissButton.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, 38f);
        buttonRect.sizeDelta = new Vector2(180f, 44f);
        dismissButton.onClick.AddListener(() => instructionsPanel.SetActive(false));
    }

    private static TextMeshProUGUI CreateText(string objectName, Transform parent, string text, float fontSize, FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI textComponent = textObject.GetComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.color = Color.white;
        textComponent.raycastTarget = false;

        return textComponent;
    }

    private static Button CreateButton(string objectName, Transform parent, string label)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = Color.white;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;

        TextMeshProUGUI buttonText = CreateText("Text", buttonObject.transform, label, 18f, FontStyles.Bold);
        RectTransform textRect = buttonText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        buttonText.color = Color.black;
        buttonText.alignment = TextAlignmentOptions.Center;

        return button;
    }
}
