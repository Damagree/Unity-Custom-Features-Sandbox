#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class TextureCompressionScript : MonoBehaviour {
    private static AddressableAssetSettings settings;

    [MenuItem("PDKT Build/Addressable/Build Addressables with DXT and ASTC")]
    public static void BuildAddressablesWithDifferentCompression() {
        settings = AddressableAssetSettingsDefaultObject.Settings;

        foreach (AddressableAssetGroup group in settings.groups) {
            // Skip processing for "Mobile" and "Desktop" groups
            if (group.Name.Equals("Mobile") || group.Name.Equals("Desktop"))
                continue;

            ProcessGroupEntries(group);
        }

        AddLabel();

        // Build Addressables
        //AddressableAssetSettings.BuildPlayerContent();

    }

    private static void AddLabel() {
        // Add label to all entries in group Mobile and Desktop
        AddressableAssetGroup desktopGroup = settings.FindGroup("Desktop");
        AddressableAssetGroup mobileGroup = settings.FindGroup("Mobile");

        foreach (AddressableAssetEntry entry in desktopGroup.entries) {
            // get the name of the original prefab, without _DXT or _ASTC
            string originalPrefabName = entry.address.Replace("_DXT", "").Replace("_ASTC", "");

            // check if the original prefab has another label
            AddressableAssetEntry originalPrefabEntry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(originalPrefabName));

            // if the original prefab has another label, add it to the new prefab
            if (originalPrefabEntry != null) {
                string[] labels = originalPrefabEntry.labels.ToArray();
                foreach (var label in labels) {
                    entry.SetLabel(label, true, true);
                }
            }

            // Add additional label based on group
            if (desktopGroup.Name.Equals("Desktop") || desktopGroup.Name.Equals("Mobile")) {
                entry.SetLabel(desktopGroup.Name, true, true);
            }
        }

        foreach (AddressableAssetEntry entry in mobileGroup.entries) {
            // get the name of the original prefab, without _DXT or _ASTC
            string originalPrefabName = entry.address.Replace("_DXT", "").Replace("_ASTC", "");

            // check if the original prefab has another label
            AddressableAssetEntry originalPrefabEntry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(originalPrefabName));

            // if the original prefab has another label, add it to the new prefab
            if (originalPrefabEntry != null) {
                string[] labels = originalPrefabEntry.labels.ToArray();
                foreach (var label in labels) {
                    entry.SetLabel(label, true, true);
                }
            }

            // Add additional label based on group
            if (desktopGroup.Name.Equals("Desktop") || desktopGroup.Name.Equals("Mobile")) {
                entry.SetLabel(mobileGroup.Name, true, true);
            }
        }
    }

    private static void ProcessGroupEntries(AddressableAssetGroup sourceGroup) {
        AddressableAssetGroup desktopGroup = GetOrCreateGroup(settings, "Desktop");
        AddressableAssetGroup mobileGroup = GetOrCreateGroup(settings, "Mobile");

        List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>(sourceGroup.entries);

        foreach (AddressableAssetEntry entry in entries) {
            if (!entry.AssetPath.EndsWith(".prefab"))
                continue;

            ProcessPrefab(entry.AssetPath, desktopGroup, mobileGroup);
        }
    }

    private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName) {
        AddressableAssetGroup group = settings.FindGroup(groupName);

        if (group != null)
            return group;

        return settings.CreateGroup(groupName, false, false, true, settings.DefaultGroup.Schemas);
    }

    private static void ProcessPrefab(string prefabPath, AddressableAssetGroup desktopGroup, AddressableAssetGroup mobileGroup) {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null) {
            Debug.LogError($"Prefab not found at path: {prefabPath}");
            return;
        }

        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
            ProcessMaterials(renderer.sharedMaterials, prefabPath, desktopGroup, mobileGroup);
    }

    private static void ProcessMaterials(Material[] materials, string prefabPath, AddressableAssetGroup desktopGroup, AddressableAssetGroup mobileGroup) {
        foreach (Material material in materials) {
            if (material == null) {
                Debug.LogWarning("Material is null in prefab: " + prefabPath);
                continue;
            }

            var textures = material.GetTexturePropertyNames();

            foreach (var textureName in textures) {
                Texture texture = material.GetTexture(textureName);

                if (texture != null && IsSupportedTextureType(AssetDatabase.GetAssetPath(texture))) {
                    string newPrefabPathDXT = prefabPath.Replace(".prefab", "_DXT.prefab");
                    string newMaterialPathDXT = AssetDatabase.GetAssetPath(material).Replace(".mat", "_DXT.mat");

                    string newPrefabPathASTC = prefabPath.Replace(".prefab", "_ASTC.prefab");
                    string newMaterialPathASTC = AssetDatabase.GetAssetPath(material).Replace(".mat", "_ASTC.mat");

                    // Process DXT
                    string newTexturePathDXT;
                    if (textureName.Contains("_BumpMap")) {
                        newTexturePathDXT = DuplicateAndCompressTexture(AssetDatabase.GetAssetPath(texture), "DXT", TextureImporterFormat.DXT5, true);
                    } else {
                        newTexturePathDXT = DuplicateAndCompressTexture(AssetDatabase.GetAssetPath(texture), "DXT", TextureImporterFormat.DXT5, false);
                    }
                    DuplicateAndUpdateMaterial(prefabPath, material, AssetDatabase.GetAssetPath(texture), textureName, newPrefabPathDXT, newMaterialPathDXT, newTexturePathDXT, desktopGroup);

                    // Process ASTC
                    string newTexturePathASTC;
                    if (textureName.Contains("_BumpMap")) {
                        newTexturePathASTC = DuplicateAndCompressTexture(AssetDatabase.GetAssetPath(texture), "ASTC", TextureImporterFormat.ASTC_4x4, true);
                    } else {
                        newTexturePathASTC = DuplicateAndCompressTexture(AssetDatabase.GetAssetPath(texture), "ASTC", TextureImporterFormat.ASTC_4x4, false);
                    }
                    DuplicateAndUpdateMaterial(prefabPath, material, AssetDatabase.GetAssetPath(texture), textureName, newPrefabPathASTC, newMaterialPathASTC, newTexturePathASTC, mobileGroup);
                }
            }
        }
    }

    private static void DuplicateAndUpdateMaterial(string prefabPath, Material originalMaterial, string originalTexturePath, string textureName,
    string newPrefabPath, string newMaterialPath, string newTexturePath, AddressableAssetGroup group) {
        // Check if material with the same name already exists
        List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>(group.entries);
        string existingMaterialGUID = entries.Find(entry => entry.address.EndsWith(newMaterialPath))?.guid;

        Material newMaterial = existingMaterialGUID != null ?
            AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(existingMaterialGUID)) :
            GetOrCreateAsset<Material>(newMaterialPath, AssetDatabase.GetAssetPath(originalMaterial));

        if (newMaterial == null) {
            Debug.LogError("Failed to create or load material: " + newMaterialPath);
            return;
        }

        // Check if texture with the same name already exists
        string newTexturePathDXT;

        // Use existing texture if available
        if (newTexturePath.Contains("ASTC") || newTexturePath.Contains("DXT")) {
            newTexturePathDXT = newTexturePath;
        } else {
            if (textureName.Contains("_BumpMap")) {
                newTexturePathDXT = DuplicateAndCompressTexture(newTexturePath, "DXT", TextureImporterFormat.DXT5, true);
            } else {
                newTexturePathDXT = DuplicateAndCompressTexture(newTexturePath, "DXT", TextureImporterFormat.DXT5, false);
            }
        }

        try {
            // Check if prefab with the same name already exists
            string existingPrefabGUID = entries.Find(entry => entry.address.EndsWith(newPrefabPath))?.guid;

            GameObject newPrefab = existingPrefabGUID != null ?
                AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(existingPrefabGUID)) :
                GetOrCreateAsset<GameObject>(newPrefabPath, prefabPath);

            if (newPrefab == null) {
                Debug.LogError("Failed to create or load prefab: " + newPrefabPath);
                return;
            }

            Renderer newRenderer = newPrefab.GetComponentInChildren<Renderer>();
            newRenderer.sharedMaterial = newMaterial;

            // Move or create prefab entry if it doesn't exist
            if (existingPrefabGUID == null) {
                string newPrefabGUID = AssetDatabase.AssetPathToGUID(newPrefabPath);

                AddressableAssetEntry prefabEntry = settings.FindAssetEntry(newPrefabGUID);
                if (prefabEntry == null) {
                    prefabEntry = settings.CreateOrMoveEntry(newPrefabGUID, group);
                }

                // Add labels from the original prefab to the new prefab entry
                GUID originalPrefabGUID = new GUID(AssetDatabase.AssetPathToGUID(prefabPath));
                string[] prefabLabels = AssetDatabase.GetLabels(originalPrefabGUID);
                foreach (var label in prefabLabels) {
                    prefabEntry.SetLabel(label, true, true);
                }

                // Add additional label based on group
                if (group.Name.Equals("Desktop") || group.Name.Equals("Mobile")) {
                    prefabEntry.SetLabel(group.Name, true, true);
                }
            }

        } catch (System.Exception e) {
            Debug.LogError($"Failed to set texture or labels: {e}");
        }
    }



    private static T GetOrCreateAsset<T>(string assetPath, string originalAssetPath) where T : Object {
        T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

        if (asset != null)
            return asset;

        AssetDatabase.CopyAsset(originalAssetPath, assetPath);
        return AssetDatabase.LoadAssetAtPath<T>(assetPath);
    }

    internal static string DuplicateAndCompressTexture(string texturePath, string suffix, TextureImporterFormat format, bool isNormalMap = false) {
        string newPath = "";

        // Check if texture with the same suffix already exists
        if (texturePath.Contains("_" + suffix))
            return texturePath;
        else
            newPath = texturePath.Replace(".png", "_" + suffix + ".png").Replace(".jpg", "_" + suffix + ".jpg").Replace(".jpeg", "_" + suffix + ".jpeg");

        // Check if texture with the same suffix and format already exists
        string existingTextureGUID = settings.FindAssetEntry(newPath)?.guid;

        if (existingTextureGUID != null)
            return newPath;

        AssetDatabase.CopyAsset(texturePath, newPath);

        TextureImporter importer = AssetImporter.GetAtPath(newPath) as TextureImporter;

        if (isNormalMap) {
            importer.textureType = TextureImporterType.NormalMap;
        } else {
            importer.textureType = TextureImporterType.Default;
        }
        importer.textureCompression = TextureImporterCompression.Compressed;
        var platformSettings = importer.GetPlatformTextureSettings("WebGL");
        platformSettings.overridden = true;
        platformSettings.maxTextureSize = 1024;
        platformSettings.format = format;
        importer.SetPlatformTextureSettings(platformSettings);
        importer.SaveAndReimport();

        return newPath;
    }

    private static bool IsSupportedTextureType(string texturePath) {
        return texturePath.EndsWith(".png") || texturePath.EndsWith(".jpg") || texturePath.EndsWith(".jpeg");
    }
}
#endif
