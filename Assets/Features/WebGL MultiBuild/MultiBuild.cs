#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;

public class MultiBuild
{
    //This creates a menu item to trigger the dual builds https://docs.unity3d.com/ScriptReference/MenuItem.html
    [MenuItem("Game Build Menu/Dual Build")]
    public static void BuildGame() {
        //This builds the player twice: a build with desktop-specific texture settings (WebGL_Build) as well as mobile-specific texture settings (WebGL_Mobile), and combines the necessary files into one directory (WebGL_Build)
        string dualBuildPath = "Build";
        string desktopBuildName = "WebGL_Build";
        string mobileBuildName = "WebGL_Mobile";

        string desktopPath = Path.Combine(dualBuildPath, desktopBuildName);
        string mobilePath = Path.Combine(dualBuildPath, mobileBuildName);
        List<string> scenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes) {
            if (scene.enabled)
                scenes.Add(scene.path);
        }

        EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.DXT;
        BuildPlayerOptions buildPlayerDesktopOption = new() {
            scenes = scenes.ToArray(),
            locationPathName = desktopPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };
        BuildPipeline.BuildPlayer(buildPlayerDesktopOption);


        EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.ASTC;
        BuildPlayerOptions buildPlayerOptions = new() {
            scenes = scenes.ToArray(),
            locationPathName = mobilePath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };
        BuildPipeline.BuildPlayer(buildPlayerOptions);

        // Copy the mobile.data file to the desktop build directory to consolidate them both
        string mobileDataSourcePath = Path.Combine(mobilePath, "Build", mobileBuildName + ".data.unityweb");
        string desktopDataDestPath = Path.Combine(desktopPath, "Build", mobileBuildName + ".data.unityweb");

        // Copy the mobile.data file
        FileUtil.CopyFileOrDirectory(mobileDataSourcePath, desktopDataDestPath);
    }

    [MenuItem("Game Build Menu/Dual Clean")]
    public static void BuildAndRunGame() {
        //This builds the player twice: a build with desktop-specific texture settings (WebGL_Build) as well as mobile-specific texture settings (WebGL_Mobile), and combines the necessary files into one directory (WebGL_Build)
        string dualBuildPath = "Build";
        string desktopBuildName = "WebGL_Build";
        string mobileBuildName = "WebGL_Mobile";

        string desktopPath = Path.Combine(dualBuildPath, desktopBuildName);
        string mobilePath = Path.Combine(dualBuildPath, mobileBuildName);
        List<string> scenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes) {
            if (scene.enabled)
                scenes.Add(scene.path);
        }


        EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.DXT;
        BuildPlayerOptions buildPlayerDesktopOption = new() {
            scenes = scenes.ToArray(),
            locationPathName = desktopPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.CleanBuildCache | BuildOptions.AutoRunPlayer,
        };
        BuildPipeline.BuildPlayer(buildPlayerDesktopOption);

        
        EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.ASTC;
        BuildPlayerOptions buildPlayerOptions = new() {
            scenes = scenes.ToArray(),
            locationPathName = mobilePath,
            target = BuildTarget.WebGL,
            options = BuildOptions.CleanBuildCache | BuildOptions.AutoRunPlayer,
        };
        BuildPipeline.BuildPlayer(buildPlayerOptions);

        // Copy the mobile.data file to the desktop build directory to consolidate them both
        string mobileDataSourcePath = Path.Combine(mobilePath, "Build", mobileBuildName + ".data.unityweb");
        string desktopDataDestPath = Path.Combine(desktopPath, "Build", mobileBuildName + ".data.unityweb");

        // Copy the mobile.data file
        FileUtil.CopyFileOrDirectory(mobileDataSourcePath, desktopDataDestPath);
    }
}
#endif