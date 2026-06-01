using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private GameObject instructionsPanel;
    private GameObject trackingControlsPanel;

    private void Start()
    {
        CreateTrackingControlsPanelIfNeeded();
    }

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

    private void CreateTrackingControlsPanelIfNeeded()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName != "HandTrackingScene" && sceneName != "WholeBodyTrackingScene")
        {
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            Debug.LogWarning("Tracking controls could not be created because no Canvas was found.");
            return;
        }

        trackingControlsPanel = new GameObject("TrackingControlsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        trackingControlsPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = trackingControlsPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-16f, -64f);
        panelRect.sizeDelta = new Vector2(168f, 86f);

        Image panelImage = trackingControlsPanel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.54f);

        if (sceneName == "HandTrackingScene")
        {
            CreateHandTrackingControls(trackingControlsPanel.transform);
            return;
        }

        CreateWholeBodyTrackingControls(trackingControlsPanel.transform);
    }

    private void CreateHandTrackingControls(Transform parent)
    {
        TrackedHandModelController handModel = FindFirstObjectByType<TrackedHandModelController>(FindObjectsInactive.Include);
        HandSkeletonVisualizer handVisualizer = FindFirstObjectByType<HandSkeletonVisualizer>(FindObjectsInactive.Include);

        Toggle modelToggle = CreateTrackingToggle("ModelViewToggle", parent, "3D Model", 0, handModel == null || handModel.ModelViewEnabled);
        modelToggle.interactable = handModel != null;

        if (handModel != null)
        {
            modelToggle.onValueChanged.AddListener(handModel.SetModelViewEnabled);
        }

        Toggle debugToggle = CreateTrackingToggle("DebugLinesToggle", parent, "Debug Lines", 1, handVisualizer == null || handVisualizer.DebugLinesVisible);
        debugToggle.interactable = handVisualizer != null;

        if (handVisualizer != null)
        {
            debugToggle.onValueChanged.AddListener(handVisualizer.SetDebugLinesVisible);
        }
    }

    private void CreateWholeBodyTrackingControls(Transform parent)
    {
        SkeletonRegionDisplayController skeletonDisplay = FindFirstObjectByType<SkeletonRegionDisplayController>(FindObjectsInactive.Include);
        BodyJointVisualizer bodyVisualizer = FindFirstObjectByType<BodyJointVisualizer>(FindObjectsInactive.Include);

        Toggle modelToggle = CreateTrackingToggle("ModelViewToggle", parent, "3D Model", 0, skeletonDisplay == null || skeletonDisplay.ModelViewEnabled);
        modelToggle.interactable = skeletonDisplay != null;

        if (skeletonDisplay != null)
        {
            modelToggle.onValueChanged.AddListener(skeletonDisplay.SetModelViewEnabled);
        }

        Toggle debugToggle = CreateTrackingToggle("DebugLinesToggle", parent, "Debug Lines", 1, bodyVisualizer == null || bodyVisualizer.DebugLinesVisible);
        debugToggle.interactable = bodyVisualizer != null;

        if (bodyVisualizer != null)
        {
            debugToggle.onValueChanged.AddListener(bodyVisualizer.SetDebugLinesVisible);
        }
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
        body.textWrappingMode = TextWrappingModes.Normal;

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

    private static Toggle CreateTrackingToggle(string objectName, Transform parent, string label, int index, bool isOn)
    {
        GameObject toggleObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle));
        toggleObject.transform.SetParent(parent, false);

        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0f, 1f);
        toggleRect.anchorMax = new Vector2(1f, 1f);
        toggleRect.pivot = new Vector2(0.5f, 1f);
        toggleRect.anchoredPosition = new Vector2(0f, -8f - index * 36f);
        toggleRect.sizeDelta = new Vector2(-16f, 30f);

        Image rowImage = toggleObject.GetComponent<Image>();
        rowImage.color = new Color(1f, 1f, 1f, 0f);

        GameObject boxObject = new GameObject("Box", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        boxObject.transform.SetParent(toggleObject.transform, false);

        RectTransform boxRect = boxObject.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0f, 0.5f);
        boxRect.anchorMax = new Vector2(0f, 0.5f);
        boxRect.pivot = new Vector2(0f, 0.5f);
        boxRect.anchoredPosition = new Vector2(8f, 0f);
        boxRect.sizeDelta = new Vector2(22f, 22f);

        Image boxImage = boxObject.GetComponent<Image>();
        boxImage.color = new Color(1f, 1f, 1f, 0.92f);

        GameObject checkObject = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        checkObject.transform.SetParent(boxObject.transform, false);

        RectTransform checkRect = checkObject.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.pivot = new Vector2(0.5f, 0.5f);
        checkRect.anchoredPosition = Vector2.zero;
        checkRect.sizeDelta = new Vector2(14f, 14f);

        Image checkImage = checkObject.GetComponent<Image>();
        checkImage.color = new Color(0.12f, 0.72f, 0.32f, 1f);

        TextMeshProUGUI toggleText = CreateText("Label", toggleObject.transform, label, 14f, FontStyles.Bold);
        RectTransform labelRect = toggleText.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(38f, 0f);
        labelRect.offsetMax = new Vector2(-6f, 0f);
        toggleText.alignment = TextAlignmentOptions.MidlineLeft;

        Toggle toggle = toggleObject.GetComponent<Toggle>();
        toggle.targetGraphic = boxImage;
        toggle.graphic = checkImage;
        toggle.isOn = isOn;

        return toggle;
    }
}
