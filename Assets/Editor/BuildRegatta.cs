using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Standalone player for the Regatta scenario: lets the demo run without the
// editor (no Bee FD crashes, no serialized-field surprises, warm shaders).
// CLI: Unity -batchmode -quit -projectPath <proj> -executeMethod BuildRegatta.Build
//
// Everything here is #if-guarded per host OS. This file lives in Assets/Editor/,
// so it compiles into Assembly-CSharp-Editor -- and one compile error there stops
// the whole editor, Play mode included. Referencing UnityEditor.OSXStandalone on a
// Linux host is exactly that error (CS0234: the macOS build-support module is not
// installed), so the Regatta scene could not be played at all on Ubuntu.
public static class BuildRegatta
{
#if UNITY_EDITOR_OSX
    [MenuItem("LOTUSim/Build Regatta (macOS)")]
    public static void Build()
    {
        // Native Apple Silicon: the player must not pay the Rosetta tax the
        // gz/xdyn containers already pay.
        UnityEditor.OSXStandalone.UserBuildSettings.architecture =
            UnityEditor.Build.OSArchitecture.ARM64;

        BuildFor(BuildTarget.StandaloneOSX, "Builds/Regatta.app");
    }
#endif

#if UNITY_EDITOR_LINUX
    [MenuItem("LOTUSim/Build Regatta (Linux)")]
    public static void Build()
    {
        BuildFor(BuildTarget.StandaloneLinux64, "Builds/Regatta/Regatta.x86_64");
    }
#endif

    static void BuildFor(BuildTarget target, string locationPathName)
    {
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Regatta/Regatta.unity" },
            locationPathName = locationPathName,
            target = target,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        Debug.Log($"[BuildRegatta] {summary.result} — {summary.totalSize / (1024 * 1024)} MB, " +
                  $"{summary.totalErrors} errors, {summary.totalWarnings} warnings, {summary.totalTime}");

        if (summary.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
