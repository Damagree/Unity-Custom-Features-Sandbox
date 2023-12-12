#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.AddressableAssets;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using System.IO;

public class MultiBuild {
    [MenuItem("PDKT Build/Player/Dual Build")]
    public static void BuildGame() {
        string dualBuildPath = "Build";
        string desktopBuildName = "WebGL_Build";
        string mobileBuildName = "WebGL_Mobile";

        List<string> scenes = GetEnabledScenePaths();

        // 1. Build Addressables with ASTC textures
        BuildWithAddressables(dualBuildPath, scenes, mobileBuildName, WebGLTextureSubtarget.ASTC);

        // 2. Build Player with ASTC textures
        BuildPlayer(dualBuildPath, scenes, mobileBuildName, WebGLTextureSubtarget.ASTC);

        // 3. Build Addressables with DXT textures
        BuildWithAddressables(dualBuildPath, scenes, desktopBuildName, WebGLTextureSubtarget.DXT);

        // 4. Build Player with DXT textures
        BuildPlayer(dualBuildPath, scenes, desktopBuildName, WebGLTextureSubtarget.DXT);
    }

    [MenuItem("PDKT Build/Player/Dual Clean")]
    public static void BuildAndRunGame() {
        string dualBuildPath = "Build";
        string desktopBuildName = "WebGL_Build";
        string mobileBuildName = "WebGL_Mobile";

        List<string> scenes = GetEnabledScenePaths();

        // 1. Clean build with Addressables and ASTC textures
        BuildWithAddressables(dualBuildPath, scenes, mobileBuildName, WebGLTextureSubtarget.ASTC);

        // 2. Clean build with Player and ASTC textures
        BuildPlayer(dualBuildPath, scenes, mobileBuildName, WebGLTextureSubtarget.ASTC);

        // 3. Clean build with Addressables and DXT textures
        BuildWithAddressables(dualBuildPath, scenes, desktopBuildName, WebGLTextureSubtarget.DXT);

        // 4. Clean build with Player and DXT textures
        BuildPlayer(dualBuildPath, scenes, desktopBuildName, WebGLTextureSubtarget.DXT);
    }

    private static List<string> GetEnabledScenePaths() {
        List<string> scenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes) {
            if (scene.enabled)
                scenes.Add(scene.path);
        }
        return scenes;
    }

    private static void BuildWithAddressables(string outputPath, List<string> scenes, string buildName, WebGLTextureSubtarget subtarget) {
        EditorUserBuildSettings.webGLBuildSubtarget = subtarget;

        AddressableAssetSettings.CleanPlayerContent(AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder);

        // Build the Addressables
        AddressableAssetSettings.BuildPlayerContent();

        // Get the path after building Addressables
        string addressablesBuildPath = AddressableAssetSettingsDefaultObject.Settings.buildSettings.bundleBuildPath;
        Debug.Log("Addressables build path: " + addressablesBuildPath);
    }


    private static void BuildPlayer(string outputPath, List<string> scenes, string buildName, WebGLTextureSubtarget subtarget) {
        EditorUserBuildSettings.webGLBuildSubtarget = subtarget;

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions {
            scenes = scenes.ToArray(),
            locationPathName = Path.Combine(outputPath, buildName),
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };

        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
}
#endif
