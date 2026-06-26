using UnityEngine;

internal sealed class SkeletonRegionPoseSmoother
{
    private bool initialized;
    private Vector3 currentPosition;
    private Quaternion currentRotation = Quaternion.identity;
    private Vector3 currentScale = Vector3.one;

    public void Reset()
    {
        initialized = false;
    }

    public void Apply(
        Transform target,
        SkeletonRegionPlacement targetPlacement,
        float positionSmoothing,
        float rotationSmoothing,
        float scaleSmoothing)
    {
        if (target == null)
        {
            return;
        }

        if (!initialized)
        {
            Initialize(targetPlacement);
        }
        else
        {
            Smooth(targetPlacement, positionSmoothing, rotationSmoothing, scaleSmoothing);
        }

        target.SetPositionAndRotation(currentPosition, currentRotation);
        target.localScale = currentScale;
    }

    private void Initialize(SkeletonRegionPlacement targetPlacement)
    {
        currentPosition = targetPlacement.Position;
        currentRotation = targetPlacement.Rotation;
        currentScale = targetPlacement.Scale;
        initialized = true;
    }

    private void Smooth(
        SkeletonRegionPlacement targetPlacement,
        float positionSmoothing,
        float rotationSmoothing,
        float scaleSmoothing)
    {
        currentPosition = Vector3
            .Lerp(currentPosition, targetPlacement.Position, Time.deltaTime * positionSmoothing);
        
        currentRotation = Quaternion
            .Slerp(currentRotation, targetPlacement.Rotation, Time.deltaTime * rotationSmoothing);
        
        currentScale = Vector3
            .Lerp(currentScale, targetPlacement.Scale, Time.deltaTime * scaleSmoothing);
    }
}
