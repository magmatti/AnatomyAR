[System.Serializable]
public class BodyJointIndexMapping
{
    public BodyJointType jointType;
    public int arKitJointIndex;

    public BodyJointIndexMapping(BodyJointType jointType, int arKitJointIndex)
    {
        this.jointType = jointType;
        this.arKitJointIndex = arKitJointIndex;
    }
}
