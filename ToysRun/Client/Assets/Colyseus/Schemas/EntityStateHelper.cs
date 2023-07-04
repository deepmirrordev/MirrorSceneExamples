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
}
