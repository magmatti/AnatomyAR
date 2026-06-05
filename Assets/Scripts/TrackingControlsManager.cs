using UnityEngine;
using UnityEngine.UI;

public class TrackingControlsManager : MonoBehaviour
{
    [SerializeField] private Toggle modelViewToggle;
    [SerializeField] private Toggle debugLinesToggle;
    [SerializeField] private TrackedHandModelController handModelController;
    [SerializeField] private HandSkeletonVisualizer handSkeletonVisualizer;
    [SerializeField] private SkeletonRegionDisplayController skeletonRegionDisplayController;
    [SerializeField] private BodyJointVisualizer bodyJointVisualizer;

    private void Start()
    {
        BindModelViewToggle();
        BindDebugLinesToggle();
    }

    private void BindModelViewToggle()
    {
        if (modelViewToggle == null)
        {
            return;
        }

        if (handModelController != null)
        {
            modelViewToggle.SetIsOnWithoutNotify(handModelController.ModelViewEnabled);
            modelViewToggle.onValueChanged.AddListener(handModelController.SetModelViewEnabled);
            return;
        }

        if (skeletonRegionDisplayController != null)
        {
            modelViewToggle.SetIsOnWithoutNotify(skeletonRegionDisplayController.ModelViewEnabled);
            modelViewToggle.onValueChanged.AddListener(skeletonRegionDisplayController.SetModelViewEnabled);
            return;
        }

        modelViewToggle.interactable = false;
    }

    private void BindDebugLinesToggle()
    {
        if (debugLinesToggle == null)
        {
            return;
        }

        if (handSkeletonVisualizer != null)
        {
            debugLinesToggle.SetIsOnWithoutNotify(handSkeletonVisualizer.DebugLinesVisible);
            debugLinesToggle.onValueChanged.AddListener(handSkeletonVisualizer.SetDebugLinesVisible);
            return;
        }

        if (bodyJointVisualizer != null)
        {
            debugLinesToggle.SetIsOnWithoutNotify(bodyJointVisualizer.DebugLinesVisible);
            debugLinesToggle.onValueChanged.AddListener(bodyJointVisualizer.SetDebugLinesVisible);
            return;
        }

        debugLinesToggle.interactable = false;
    }
}
