using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PersistentObjectId : MonoBehaviour
{
    [SerializeField] private string id;

    public string Id => id;

    private void Reset()
    {
        GenerateNewId();
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
            GenerateNewId();
    }

    [ContextMenu("Generate New Persistent ID")]
    public void GenerateNewId()
    {
        id = Guid.NewGuid().ToString("N");
    }

    public void AssignRuntimeId(string runtimeId)
    {
        if (!Application.isPlaying)
            return;

        id = runtimeId;
    }
}
