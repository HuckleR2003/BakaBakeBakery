using System;
using System.IO;
using System.IO.Compression;
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
        private const string WebOutputDirectory = "Builds/Browser";
        private const string ReleaseDirectory = "Builds/Release";
        private const string ExecutableName = "BakaBakeBakery.exe";

        public static string ExecutablePath => Path.GetFullPath(
            Path.Combine(OutputDirectory, ExecutableName));

        [MenuItem("Baka Bake Bakery/Build Windows Player")]
        public static void BuildWindows()
        {
            Directory.CreateDirectory(OutputDirectory);
            var options = new BuildPlayerOptions
            {
                scenes = EnabledScenes(),
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

            WriteItchManifest();
        }

        /// <summary>
        /// Builds the player and wraps it in the archive the itch.io page expects: one zip whose
        /// root holds the executable, its data folder and the app manifest, with the debug symbol
        /// folder left behind.
        /// </summary>
        [MenuItem("Baka Bake Bakery/Package for itch.io")]
        public static void PackageForItch()
        {
            BuildWindows();

            var version = PlayerSettings.bundleVersion;
            Directory.CreateDirectory(ReleaseDirectory);
            var archivePath = Path.Combine(ReleaseDirectory, $"baka-bake-bakery-windows-{version}.zip");
            if (File.Exists(archivePath))
            {
                File.Delete(archivePath);
            }

            var staging = Path.Combine(ReleaseDirectory, "staging-windows");
            if (Directory.Exists(staging))
            {
                Directory.Delete(staging, true);
            }

            CopyShippingFiles(OutputDirectory, staging);
            ZipFile.CreateFromDirectory(staging, archivePath, System.IO.Compression.CompressionLevel.Optimal, false);
            Directory.Delete(staging, true);

            var size = new FileInfo(archivePath).Length;
            Debug.Log($"[Baka Bake Bakery] ITCH_PACKAGE_READY {Path.GetFullPath(archivePath)} ({size / (1024 * 1024)} MB).");
        }

        private static void CopyShippingFiles(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                if (IsExcluded(directory))
                {
                    continue;
                }

                Directory.CreateDirectory(directory.Replace(source, destination));
            }

            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                if (IsExcluded(file))
                {
                    continue;
                }

                File.Copy(file, file.Replace(source, destination), true);
            }
        }

        private static bool IsExcluded(string path)
        {
            return path.Contains("BurstDebugInformation_DoNotShip", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith("UnityCrashHandler64.exe", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Builds the browser player with the two settings an itch.io page needs. itch serves the
        /// upload without a Content-Encoding header, so the build has to be able to unpack itself:
        /// gzip plus the embedded decompression fallback. Brotli would be smaller but needs server
        /// headers that itch does not expose. See Docs/ItchRelease.md.
        /// </summary>
        [MenuItem("Baka Bake Bakery/Package for itch.io (Browser)")]
        public static void PackageBrowserForItch()
        {
            var scenes = EnabledScenes();
            ConfigureWebPublishing();

            Directory.CreateDirectory(WebOutputDirectory);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = WebOutputDirectory,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None
            });

            var summary = report.summary;
            Debug.Log($"[Baka Bake Bakery] Web Build Finished, Result: {summary.result}, Size: {summary.totalSize} bytes.");
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"WebGL build failed with result '{summary.result}'.");
            }

            var indexPath = Path.Combine(WebOutputDirectory, "index.html");
            if (!File.Exists(indexPath))
            {
                throw new InvalidOperationException("The browser build has no index.html at its root; itch.io cannot play it.");
            }

            Directory.CreateDirectory(ReleaseDirectory);
            var archivePath = Path.Combine(
                ReleaseDirectory,
                $"baka-bake-bakery-browser-{PlayerSettings.bundleVersion}.zip");
            if (File.Exists(archivePath))
            {
                File.Delete(archivePath);
            }

            ZipFile.CreateFromDirectory(
                WebOutputDirectory,
                archivePath,
                System.IO.Compression.CompressionLevel.Optimal,
                false);

            var size = new FileInfo(archivePath).Length;
            Debug.Log($"[Baka Bake Bakery] ITCH_WEB_PACKAGE_READY {Path.GetFullPath(archivePath)} ({size / (1024 * 1024)} MB).");
        }

        private static void ConfigureWebPublishing()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.template = "APPLICATION:Default";
            PlayerSettings.defaultWebScreenWidth = 1600;
            PlayerSettings.defaultWebScreenHeight = 900;
            AssetDatabase.SaveAssets();
        }

        private static string[] EnabledScenes()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("No shipping scenes are enabled in the build settings.");
            }

            return scenes;
        }

        /// <summary>
        /// The itch app reads this to know what to launch, so players get a Play button instead of
        /// a folder of files. See https://itch.io/docs/itch/integrating/manifest.html
        /// </summary>
        private static void WriteItchManifest()
        {
            var manifest = string.Join(
                Environment.NewLine,
                "# itch.io app manifest — https://itch.io/docs/itch/integrating/manifest.html",
                "[[actions]]",
                "name = \"play\"",
                $"path = \"{ExecutableName}\"",
                "",
                "[[actions]]",
                "name = \"Report a problem\"",
                "path = \"https://github.com/HuckleR2003/BakaBakeBakery/issues\"",
                "");
            File.WriteAllText(Path.Combine(OutputDirectory, ".itch.toml"), manifest);
        }
    }
}
