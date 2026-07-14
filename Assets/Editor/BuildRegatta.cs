using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Standalone player for the Regatta scenario: lets the demo run without the
// editor (no Bee FD crashes, no serialized-field surprises, warm shaders).
// CLI: Unity -batchmode -quit -projectPath <proj> -executeMethod BuildRegatta.Build
public static class BuildRegatta
{
    [MenuItem("LOTUSim/Build Regatta (macOS)")]
    public static void Build()
    {
        // Native Apple Silicon: the player must not pay the Rosetta tax the
        // gz/xdyn containers already pay.
        UnityEditor.OSXStandalone.UserBuildSettings.architecture =
            UnityEditor.Build.OSArchitecture.ARM64;

        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Regatta/Regatta.unity" },
            locationPathName = "Builds/Regatta.app",
            target = BuildTarget.StandaloneOSX,
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
