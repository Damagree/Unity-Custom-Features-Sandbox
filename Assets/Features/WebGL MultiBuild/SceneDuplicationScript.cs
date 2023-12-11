#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using UnityEditor.SceneManagement;
using System;

public class SceneDuplicationScript {

    [MenuItem("PDKT Build/Scene/Duplicate Scene with DXT5")]
    public static void DuplicateSceneWithDXT1Crunch() {
        DuplicateSceneWithCompression(TextureFormat.DXT5, "_DXT");
    }

    [MenuItem("PDKT Build/Scene/Duplicate Scene with ASTC_8x8")]
    public static void DuplicateSceneWithASTC8x8() {
        DuplicateSceneWithCompression(TextureFormat.ASTC_8x8, "_ASTC");
    }

    private static void DuplicateSceneWithCompression(TextureFormat textureFormat, string suffix) {
        // Open a dialog to choose the target scene to duplicate
        string sourceScenePath = EditorUtility.OpenFilePanel("Select Source Scene", "Assets", "unity");

        if (string.IsNullOrEmpty(sourceScenePath)) {
            Debug.LogError("Invalid scene path");
            return;
        }

        // Duplicate the scene
        string newScenePath = DuplicateScene(sourceScenePath, textureFormat, suffix);

        if (string.IsNullOrEmpty(newScenePath)) {
            Debug.LogError("Failed to duplicate the scene");
            return;
        }

        // Load the duplicated scene
        Scene duplicatedScene = EditorSceneManager.OpenScene(newScenePath, OpenSceneMode.Single);

        if (duplicatedScene.IsValid()) {
            // Process assets in the duplicated scene
            CheckSceneAssets(duplicatedScene, textureFormat, suffix);

            // Save the duplicated scene
            EditorSceneManager.SaveScene(duplicatedScene);
        } else {
            Debug.LogError("Failed to load duplicated scene");
        }
    }

    private static string DuplicateScene(string sourceScenePath, TextureFormat textureFormat, string suffix) {
        // Create a new scene path with the target texture format appended to the file name
        string newScenePath = Path.GetDirectoryName(sourceScenePath) + "/" + Path.GetFileNameWithoutExtension(sourceScenePath) + suffix + ".unity";

        // check if scene with the same suffix already exists
        if (File.Exists(newScenePath)) {
            return newScenePath;
        }

        // Duplicate the scene
        if (EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), newScenePath)) {
            // Refresh the asset database
            AssetDatabase.Refresh();
            return newScenePath;
        }

        return null;
    }

    private static void CheckSceneAssets(Scene scene, TextureFormat textureFormat, string suffix) {
        // Iterate through all game objects in the scene
        GameObject[] rootObjects = scene.GetRootGameObjects();

        foreach (GameObject rootObject in rootObjects) {
            // Check if the game object has a Renderer component
            Renderer renderer = rootObject.GetComponent<Renderer>();

            if (renderer != null) {
                // Duplicate the game object
                GameObject duplicatedObject = GameObject.Instantiate(rootObject, rootObject.transform.position, rootObject.transform.rotation);

                // Copy the scale from the original to the duplicated object
                duplicatedObject.transform.localScale = rootObject.transform.localScale;

                // Rename the duplicated object
                duplicatedObject.name = rootObject.name + suffix;

                // Process materials, textures, or any other assets in the duplicated object
                ProcessAssetsInGameObject(duplicatedObject, textureFormat, suffix);

                // Delete the original object
                GameObject.DestroyImmediate(rootObject);
            }
        }
    }

    private static void ProcessAssetsInGameObject(GameObject gameObject, TextureFormat textureFormat, string suffix) {
        // Iterate through all components of the GameObject
        Component[] components = gameObject.GetComponents<Component>();

        foreach (Component component in components) {
            // Process materials, textures, or any other assets as needed
            ProcessAssetsInComponent(component, textureFormat, suffix);
        }
    }

    private static void ProcessAssetsInComponent(Component component, TextureFormat textureFormat, string suffix) {
        // Process materials, textures, or any other assets as needed
        if (component is Renderer renderer) {
            ProcessMaterials(renderer, renderer.sharedMaterials, textureFormat, suffix);
        }
    }

    private static void ProcessMaterials(Renderer renderer, Material[] materials, TextureFormat textureFormat, string suffix) {
        foreach (Material originalMaterial in materials) {
            if (originalMaterial != null) {
                // Duplicate the material with a new name
                Material duplicatedMaterial = DuplicateMaterial(originalMaterial, textureFormat, suffix);

                // Process textures in the material
                var textures = duplicatedMaterial.GetTexturePropertyNames();

                foreach (var textureName in textures) {
                    Texture texture = duplicatedMaterial.GetTexture(textureName);

                    // if the textureNamer is normal map, we need to set the texture type to normal map
                    string newTexturePath;
                    if (textureName.Contains("_BumpMap")) {
                        newTexturePath = ProcessTextureEntry(texture, textureFormat, suffix, isNormalMap: true);

                    } else {
                        newTexturePath = ProcessTextureEntry(texture, textureFormat, suffix, isNormalMap: false);
                    }

                    // Process the texture using the logic from the TextureCompressionScript

                    if (string.IsNullOrEmpty(newTexturePath)) {
                        continue;
                    }

                    // Set the new texture path to the material
                    duplicatedMaterial.SetTexture(textureName, AssetDatabase.LoadAssetAtPath<Texture>(newTexturePath));
                }

                // Assign the duplicated material to the renderer
                renderer.sharedMaterial = duplicatedMaterial;
            }
        }
    }

    private static Material DuplicateMaterial(Material originalMaterial, TextureFormat textureFormat, string suffix) {
        // check if original material are contain suffix
        if (originalMaterial.name.Contains(suffix)) {
            return originalMaterial;
        }

        // Create a new name for the duplicated material
        string newMaterialName = originalMaterial.name + suffix;

        // Check if material with the same name already exists
        Material existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(Path.Combine("Assets", newMaterialName + ".mat"));
        
        // If the material with the same name already exists, update the existing material
        if (existingMaterial != null && existingMaterial.name.Equals(newMaterialName)) {
            // Update the existing material
            EditorUtility.CopySerializedIfDifferent(originalMaterial, existingMaterial);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return existingMaterial;
        }

        // Create a new instance of the material (no longer using "new Material(originalMaterial)")
        Material duplicatedMaterial = UnityEngine.Object.Instantiate(originalMaterial);

        // Set the new name for the duplicated material
        duplicatedMaterial.name = newMaterialName;

        // Get the path of the original material
        string originalMaterialPath = AssetDatabase.GetAssetPath(originalMaterial);

        // Get the directory of the original material
        string originalMaterialDirectory = Path.GetDirectoryName(originalMaterialPath);

        // Construct the path for the duplicated material in the same folder
        string duplicatedMaterialPath = Path.Combine(originalMaterialDirectory, newMaterialName + ".mat");

        // Create a new asset for the duplicated material
        AssetDatabase.CreateAsset(duplicatedMaterial, duplicatedMaterialPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return duplicatedMaterial;
    }

    private static string ProcessTextureEntry(Texture texture, TextureFormat textureFormat, string suffix, bool isNormalMap = false) {
        if (texture == null) {
            return null;
        }

        TextureImporterFormat format = TextureImporterFormat.DXT5;

        if (textureFormat == TextureFormat.ASTC_8x8) {
            format = TextureImporterFormat.ASTC_8x8;
        }

        var texturePath = AssetDatabase.GetAssetPath(texture);
        if (string.IsNullOrEmpty(texturePath)) {
            Debug.LogError("Invalid texture path");
            return null;
        }

        // Use suffix provided for naming
        string newPath = texturePath.Replace(Path.GetExtension(texturePath), suffix + Path.GetExtension(texturePath));

        // Check if texture with the same suffix already exists
        if (texturePath.Contains(suffix))
            return texturePath;

        // Check if texture with the same suffix and format already exists
        var existingTextureGUIDs = AssetDatabase.FindAssets(newPath);

        if (existingTextureGUIDs.Length > 0)
            return newPath;

        AssetDatabase.CopyAsset(texturePath, newPath);

        TextureImporter importer = AssetImporter.GetAtPath(newPath) as TextureImporter;

        if(isNormalMap) {
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
}
#endif
