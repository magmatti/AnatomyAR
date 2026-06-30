using UnityEngine;
using UnityEngine.UI;

public class TrackingControlsManager : MonoBehaviour
{
    [SerializeField] private Toggle modelViewToggle;
    [SerializeField] private Toggle debugLinesToggle;
    [SerializeField] private HandModelController handModelController;
    [SerializeField] private HandSkeletonDebugVisualizer handSkeletonDebugVisualizer;
    [SerializeField] private SkeletonModelController skeletonModelController;
    [SerializeField] private BodySkeletonDebugVisualizer bodySkeletonDebugVisualizer;

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
            modelViewToggle.onValueChanged
                .AddListener(handModelController.SetModelViewEnabled);
            return;
        }

        if (skeletonModelController != null)
        {
            modelViewToggle.SetIsOnWithoutNotify(skeletonModelController.ModelViewEnabled);
            modelViewToggle.onValueChanged
                .AddListener(skeletonModelController.SetModelViewEnabled);
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

        if (handSkeletonDebugVisualizer != null)
        {
            debugLinesToggle
                .SetIsOnWithoutNotify(handSkeletonDebugVisualizer.DebugLinesVisible);
            debugLinesToggle.onValueChanged
                .AddListener(handSkeletonDebugVisualizer.SetDebugLinesVisible);
            return;
        }

        if (bodySkeletonDebugVisualizer != null)
        {
            debugLinesToggle
                .SetIsOnWithoutNotify(bodySkeletonDebugVisualizer.DebugLinesVisible);
            debugLinesToggle.onValueChanged
                .AddListener(bodySkeletonDebugVisualizer.SetDebugLinesVisible);
            return;
        }

        debugLinesToggle.interactable = false;
    }
}
