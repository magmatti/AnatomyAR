using UnityEngine;
using UnityEngine.SceneManagement;

public class NavigationManager : MonoBehaviour
{
    [SerializeField] private GameObject instructionsPanel;
    private string handTrackingSceneName = "HandTrackingScene";
    private string bodyTrackingSceneName = "BodyTrackingScene";
    private string mainMenuSceneName = "MainMenuScene";

    public void OnHandButtonPressed() => SceneManager.LoadScene(handTrackingSceneName);
    
    public void OnSkeletonButtonPressed() => SceneManager.LoadScene(bodyTrackingSceneName);

    public void OnBackButtonPressed() => SceneManager.LoadScene(mainMenuSceneName);

    public void OnInstructionsButtonPressed() => instructionsPanel.SetActive(true);
}
