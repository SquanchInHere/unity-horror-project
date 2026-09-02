using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ItemIconGenerator
{
    private const string OutputFolder = "Assets/Project/Textures/ItemIcons";

    private static readonly Queue<ItemDefinition> Queue = new();
    private static ItemDefinition currentItem;
    private static double startedAt;

    [MenuItem("Tools/Escape From The Mine/Generate Selected Item Icons")]
    private static void GenerateSelectedIcons()
    {
        Queue.Clear();

        foreach (Object selectedObject in Selection.objects)
        {
            if (selectedObject is ItemDefinition item)
                Queue.Enqueue(item);
        }

        if (Queue.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Item Icon Generator",
                "Select one or more ItemDefinitions in the window Project.",
                "OK"
            );
            return;
        }

        Directory.CreateDirectory(OutputFolder);
        currentItem = null;
        EditorApplication.update -= ProcessQueue;
        EditorApplication.update += ProcessQueue;
    }

    private static void ProcessQueue()
    {
        if (currentItem == null)
        {
            if (Queue.Count == 0)
            {
                EditorApplication.update -= ProcessQueue;
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return;
            }

            currentItem = Queue.Dequeue();
            startedAt = EditorApplication.timeSinceStartup;
        }

        GameObject sourcePrefab = currentItem.WorldPrefab != null
            ? currentItem.WorldPrefab
            : currentItem.HeldPrefab;

        if (sourcePrefab == null)
        {
            Debug.LogError(
                $"{currentItem.name}: no World Prefab or Held Prefab assigned.",
                currentItem
            );
            currentItem = null;
            return;
        }

        Texture2D preview = AssetPreview.GetAssetPreview(sourcePrefab);

        if (preview == null)
        {
            if (EditorApplication.timeSinceStartup - startedAt > 10.0)
            {
                Debug.LogError(
                    $"{currentItem.name}: Unity was unable to create a preview of the model.",
                    currentItem
                );
                currentItem = null;
            }

            return;
        }

        SaveIcon(currentItem, preview);
        currentItem = null;
    }

    private static void SaveIcon(ItemDefinition item, Texture2D preview)
    {
        const int iconSize = 256;

        RenderTexture renderTexture = RenderTexture.GetTemporary(
            iconSize,
            iconSize,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.sRGB
        );

        RenderTexture previous = RenderTexture.active;
        Graphics.Blit(preview, renderTexture);
        RenderTexture.active = renderTexture;

        Texture2D readableTexture = new Texture2D(
            iconSize,
            iconSize,
            TextureFormat.RGBA32,
            false
        );

        readableTexture.ReadPixels(
            new Rect(0, 0, iconSize, iconSize),
            0,
            0
        );
        readableTexture.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);

        string iconPath = $"{OutputFolder}/{item.name}_Icon.png";
        File.WriteAllBytes(iconPath, readableTexture.EncodeToPNG());
        Object.DestroyImmediate(readableTexture);

        AssetDatabase.ImportAsset(
            iconPath,
            ImportAssetOptions.ForceSynchronousImport
        );

        TextureImporter importer =
            AssetImporter.GetAtPath(iconPath) as TextureImporter;

        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        SerializedObject serializedItem = new SerializedObject(item);
        serializedItem.FindProperty("icon").objectReferenceValue = icon;
        serializedItem.ApplyModifiedProperties();
        EditorUtility.SetDirty(item);

        Debug.Log($"Icon created: {iconPath}", item);
    }
}
