#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using UnityEditor.SceneManagement;
using System.Linq;

public class SceneDuplicationScript {
    [MenuItem("PDKT Build/Scene/Duplicate Scene (Built-in RP) with DXT5")]
    public static void DuplicateSceneBuiltInDXT1Crunch() {
        DuplicateSceneWithCompression(TextureFormat.DXT5, "_DXT");
    }

    [MenuItem("PDKT Build/Scene/Duplicate Scene (Built-in RP) with ASTC_8x8")]
    public static void DuplicateSceneBuiltInASTC8x8() {
        DuplicateSceneWithCompression(TextureFormat.ASTC_8x8, "_ASTC");
    }

    [MenuItem("PDKT Build/Scene/Duplicate Scene (URP) with DXT5")]
    public static void DuplicateSceneURPDXT1Crunch() {
        DuplicateSceneWithCompressionURP(TextureFormat.DXT5, "_DXT");
    }

    [MenuItem("PDKT Build/Scene/Duplicate Scene (URP) with ASTC_8x8")]
    public static void DuplicateSceneURPASTC8x8() {
        DuplicateSceneWithCompressionURP(TextureFormat.ASTC_8x8, "_ASTC");
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

    private static void DuplicateSceneWithCompressionURP(TextureFormat textureFormat, string suffix) {
        // Open a dialog to choose the target scene to duplicate
        string sourceScenePath = EditorUtility.OpenFilePanel("Select Source Scene", "Assets", "unity");

        if (string.IsNullOrEmpty(sourceScenePath)) {
            Debug.LogError("Invalid scene path");
            return;
        }

        // Duplicate the scene
        string newScenePath = DuplicateSceneURP(sourceScenePath, textureFormat, suffix);

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

    private static string DuplicateSceneURP(string sourceScenePath, TextureFormat textureFormat, string suffix) {
        // Create a new scene path with the target texture format appended to the file name
        string newScenePath = Path.GetDirectoryName(sourceScenePath) + "/" + Path.GetFileNameWithoutExtension(sourceScenePath) + suffix + "_URP.unity";

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
        // Find all renderers in the scene
        Renderer[] rootRenderers = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Renderer>(true)).ToArray();

        foreach (Renderer rootRenderer in rootRenderers) {
            // Process materials, textures, or any other assets in the GameObject and its descendants
            ProcessAssetsInRendererAndDescendants(rootRenderer, textureFormat, suffix);
        }
    }

    private static void ProcessAssetsInRendererAndDescendants(Renderer renderer, TextureFormat textureFormat, string suffix) {
        // Process materials, textures, or any other assets as needed
        ProcessMaterials(renderer, renderer.sharedMaterials, textureFormat, suffix);

        // Check if the game object has any children
        if (renderer.transform.childCount > 0) {
            // Iterate through all children of the game object
            foreach (Transform child in renderer.transform) {
                Renderer childRenderer = child.GetComponent<Renderer>();

                if (childRenderer != null) {
                    // Recursively process materials, textures, or any other assets in the child Renderer and its descendants
                    ProcessAssetsInRendererAndDescendants(childRenderer, textureFormat, suffix);
                }
            }
        }
    }

    private static void ProcessMaterials(Renderer renderer, Material[] materials, TextureFormat textureFormat, string suffix) {
        List<Material> duplicatedMaterials = new List<Material>();

        foreach (Material originalMaterial in materials) {
            if (originalMaterial != null) {
                string originalMaterialName = originalMaterial.name;

                // Check if the original material name contains "_DXT" or "_ASTC"
                if (originalMaterialName.Contains("_DXT") || originalMaterialName.Contains("_ASTC")) {
                    // Do not apply the new suffix if it already contains "_DXT" or "_ASTC"
                    duplicatedMaterials.Add(originalMaterial);
                    continue;
                }

                // Create a new name for the duplicated material
                string newMaterialName = originalMaterialName + suffix;

                // Check if the material with the same name and suffix already exists
                Material existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(Path.Combine("Assets", newMaterialName + ".mat"));

                Material duplicatedMaterial = null;
                // If the material with the same name and suffix already exists, update the existing material
                if (existingMaterial != null && existingMaterial.name.Equals(newMaterialName)) {
                    // Update the existing material
                    EditorUtility.CopySerializedIfDifferent(originalMaterial, existingMaterial);
                    duplicatedMaterials.Add(existingMaterial);
                } else {
                    // Create a new instance of the material (no longer using "new Material(originalMaterial)")
                    duplicatedMaterial = UnityEngine.Object.Instantiate(originalMaterial);

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

                    duplicatedMaterials.Add(duplicatedMaterial);
                }

                // Duplicate textures and update material references
                foreach (string textureProperty in originalMaterial.GetTexturePropertyNames()) {
                    Texture originalTexture = originalMaterial.GetTexture(textureProperty);
                    if (originalTexture != null) {
                        string newTexturePath = ProcessTextureEntry(originalTexture, textureFormat, suffix);
                        if (!string.IsNullOrEmpty(newTexturePath)) {
                            Texture newTexture = AssetDatabase.LoadAssetAtPath<Texture>(newTexturePath);
                            duplicatedMaterial.SetTexture(textureProperty, newTexture);
                        }
                    }
                }
            }
        }

        // Assign the duplicated materials to the renderer
        renderer.sharedMaterials = duplicatedMaterials.ToArray();
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

    private static string ProcessTextureEntry(Texture texture, TextureFormat textureFormat, string suffix) {
        if (texture == null) {
            return null;
        }

        TextureImporterFormat format = textureFormat == TextureFormat.ASTC_8x8
            ? TextureImporterFormat.ASTC_8x8
            : TextureImporterFormat.DXT5;

        string texturePath = AssetDatabase.GetAssetPath(texture);

        if (string.IsNullOrEmpty(texturePath)) {
            Debug.LogError("Invalid texture path");
            return null;
        }

        // Check if the texture already contains the suffix
        if (texturePath.Contains(suffix)) {
            return texturePath;
        }

        // Use suffix provided for naming
        string extension = Path.GetExtension(texturePath);
        string newPath = texturePath.Replace(extension, suffix + extension);

        // Check if the texture with the same suffix already exists
        if (AssetDatabase.LoadAssetAtPath<Texture>(newPath) != null) {
            return newPath;
        }

        AssetDatabase.CopyAsset(texturePath, newPath);

        TextureImporter importer = AssetImporter.GetAtPath(newPath) as TextureImporter;

        if (importer != null) {
            importer.textureCompression = TextureImporterCompression.Compressed;
            var platformSettings = importer.GetPlatformTextureSettings("WebGL");
            platformSettings.overridden = true;
            platformSettings.maxTextureSize = 1024;
            platformSettings.format = format;
            importer.SetPlatformTextureSettings(platformSettings);
            importer.SaveAndReimport();
        } else {
            Debug.LogError($"Failed to get TextureImporter for texture: {newPath}");
        }

        return newPath;
    }
}
#endif
