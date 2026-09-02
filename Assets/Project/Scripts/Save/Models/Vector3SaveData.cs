using System;
using UnityEngine;

[Serializable]
public class Vector3SaveData
{
    public float x;
    public float y;
    public float z;

    public Vector3SaveData() { }

    public Vector3SaveData(Vector3 value)
    {
        x = value.x;
        y = value.y;
        z = value.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}
