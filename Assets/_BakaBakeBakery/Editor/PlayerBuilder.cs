using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BakaBakeBakery.Editor
{
    /// <summary>
    /// One reproducible entry point for the Windows player, so the stability run in
    /// <c>Docs/StabilityAudit.md</c> can be repeated from a command line instead of by hand.
    /// </summary>
    public static class PlayerBuilder
    {
        private const string OutputDirectory = "Builds/Windows";
        private const string ExecutableName = "BakaBakeBakery.exe";

        public static string ExecutablePath => Path.GetFullPath(
            Path.Combine(OutputDirectory, ExecutableName));

        [MenuItem("Baka Bake Bakery/Build Windows Player")]
        public static void BuildWindows()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("No shipping scenes are enabled in the build settings.");
            }

            Directory.CreateDirectory(OutputDirectory);
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(OutputDirectory, ExecutableName),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[Baka Bake Bakery] Build Finished, Result: {summary.result}, Size: {summary.totalSize} bytes.");
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Windows build failed with result '{summary.result}'.");
            }
        }
    }
}
