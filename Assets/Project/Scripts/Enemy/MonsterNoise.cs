using System;
using UnityEngine;

public static class MonsterNoise
{
    public static event Action<Vector3, float> Emitted;

    public static void Emit(Vector3 position, float hearingRadius)
    {
        if (hearingRadius <= 0.0f)
            return;

        Emitted?.Invoke(position, hearingRadius);
    }
}
