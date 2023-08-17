using Colyseus.Schema;
using UnityEngine;

public partial class EntityState : Schema {
    public Vector3 GetPosition() 
    { 
        return new Vector3(xPos, yPos, zPos); 
    }

    public Quaternion GetRotation()
    {
        return new Quaternion(xRot, yRot, zRot, wRot);
    }
    public Vector3 GetScale() 
    { 
        return new Vector3(xScale, yScale, zScale); 
    }

    public bool GetLocalizedState()
    {
        return isLocalized;
    }

    public void SetPose(Pose pose)
    {
        xPos = pose.position.x; yPos = pose.position.y; zPos = pose.position.z;
        xRot = pose.rotation.x; yRot = pose.rotation.y; zRot = pose.rotation.z; wRot = pose.rotation.w;
    }
}
