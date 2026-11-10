using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;

// A disk-backed queue survives the domain reload when switching to WebGL.
[InitializeOnLoad]
public static class ReleaseBuilds
{
    const string Request = "Temp/ctz-release-build.request";
    const string Status = "Logs/release-build.txt";
    static bool busy;

    static ReleaseBuilds() { EditorApplication.update += Tick; }

    [MenuItem("Count To Zero/Build/PC and itch.zip")]
    public static void BuildAll() { Queue("pc\nweb"); }

    [MenuItem("Count To Zero/Build/PC")]
    public static void BuildPC() { Queue("pc"); }

    [MenuItem("Count To Zero/Build/itch.zip (WebGL)")]
    public static void BuildWeb() { Queue("web"); }

    static void Queue(string targets)
    {
        if (File.Exists(Request) || busy) throw new InvalidOperationException("A release build is already queued.");
        Directory.CreateDirectory("Logs");
        File.WriteAllText(Status, "Queued: " + targets.Replace('\n', ',') + "\n");
        File.WriteAllText(Request, targets);
    }

    static void Tick()
    {
        if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Request)) return;
        busy = true;
        try
        {
            var queue = File.ReadAllLines(Request).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            if (queue.Length == 0) { File.Delete(Request); return; }
            bool web = queue[0] == "web";
            if (!web && queue[0] != "pc") throw new InvalidOperationException("Unknown build target: " + queue[0]);
            var target = web ? BuildTarget.WebGL : BuildTarget.StandaloneWindows64;
            var group = web ? BuildTargetGroup.WebGL : BuildTargetGroup.Standalone;
            if (!BuildPipeline.IsBuildTargetSupported(group, target))
                throw new InvalidOperationException("Install Unity build support for " + target);
            // Use the shared scenes and project settings, without stale profile overrides.
            if (BuildProfile.GetActiveBuildProfile() != null)
            {
                BuildProfile.SetActiveBuildProfile(null);
                return;
            }
            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                File.AppendAllText(Status, "Switching to " + target + "\n");
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(group, target))
                    throw new InvalidOperationException("Could not switch to " + target);
                return;
            }
            RemoveEditorOnlyTextShader();
            ConfigureCursor();
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultIsNativeResolution = true;
            if (web)
            {
                PlayerSettings.WebGL.template = "PROJECT:CountToZeroPWA";
                PlayerSettings.WebGL.nameFilesAsHashes = true;
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.decompressionFallback = true;
                PlayerSettings.WebGL.threadsSupport = false;
            }
            AssetDatabase.SaveAssets();
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) throw new InvalidOperationException("No enabled build scenes.");
            string output = web ? "Builds/WebGL" : "Builds/PC/CountToZero.exe";
            Directory.CreateDirectory(web ? output : "Builds/PC");
            File.AppendAllText(Status, "Building " + target + " -> " + output + "\n");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes, locationPathName = output, target = target,
                subtarget = web ? 0 : (int)StandaloneBuildSubtarget.Player,
                options = BuildOptions.None
            });
            File.AppendAllText(Status, target + ": " + report.summary.result + ", errors=" + report.summary.totalErrors + "\n");
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Build failed for " + target + ". See Logs/Editor.log.");
            if (web)
            {
                if (!File.Exists(output + "/index.html")) throw new IOException("Missing WebGL index.html.");
                string temporaryZip = "Builds/itch.pending.zip";
                if (File.Exists(temporaryZip)) File.Delete(temporaryZip);
                ZipFile.CreateFromDirectory(output, temporaryZip, System.IO.Compression.CompressionLevel.Optimal, false);
                if (File.Exists("Builds/itch.zip")) File.Delete("Builds/itch.zip");
                File.Move(temporaryZip, "Builds/itch.zip");
                File.AppendAllText(Status, "Packaged Builds/itch.zip (index.html at archive root)\n");
                File.Copy("Builds/itch.zip", "Builds/UnityPlay.zip", true);
                File.AppendAllText(Status, "Packaged Builds/UnityPlay.zip (same WebGL build)\n");
            }
            else
            {
                const string pcZip = "Builds/CountToZero-PC.zip";
                const string pending = "Builds/CountToZero-PC.pending.zip";
                if (File.Exists(pending)) File.Delete(pending);
                using (var archive = ZipFile.Open(pending, ZipArchiveMode.Create))
                {
                    foreach (var file in Directory.GetFiles("Builds/PC", "*", SearchOption.AllDirectories))
                    {
                        var relative = Path.GetRelativePath("Builds/PC", file).Replace('\\', '/');
                        if (relative.Split('/').Any(part => part.Contains("BackUpThisFolder_ButDontShipItWithYourGame"))) continue;
                        archive.CreateEntryFromFile(file, relative, System.IO.Compression.CompressionLevel.Optimal);
                    }
                }
                if (File.Exists(pcZip)) File.Delete(pcZip);
                File.Move(pending, pcZip);
                File.AppendAllText(Status, "Packaged " + pcZip + "\n");
            }
            if (queue.Length > 1) File.WriteAllLines(Request, queue.Skip(1));
            else { File.Delete(Request); File.AppendAllText(Status, "DONE\n"); }
        }
        catch (Exception ex)
        {
            File.Delete(Request);
            File.AppendAllText(Status, "FAILED: " + ex + "\n");
            Debug.LogException(ex);
        }
        finally { busy = false; }
    }

    static void RemoveEditorOnlyTextShader()
    {
        var graphics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        var shaders = graphics.FindProperty("m_AlwaysIncludedShaders");
        for (int i = shaders.arraySize - 1; i >= 0; i--)
        {
            var shader = shaders.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
            if (shader == null || shader.name != "GUI/Text Shader") continue;
            shaders.GetArrayElementAtIndex(i).objectReferenceValue = null;
            shaders.DeleteArrayElementAtIndex(i);
        }
        graphics.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ConfigureCursor()
    {
        var path = AssetDatabase.GetAssetPath(PlayerSettings.defaultCursor);
        if (string.IsNullOrEmpty(path)) return;
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        bool changed = !importer.isReadable || importer.mipmapEnabled || !importer.alphaIsTransparency;
        importer.isReadable = true;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        foreach (var platform in new[] { "Standalone", "WebGL" })
        {
            var settings = importer.GetPlatformTextureSettings(platform);
            if (settings.overridden && settings.format == TextureImporterFormat.RGBA32) continue;
            settings.overridden = true;
            settings.format = TextureImporterFormat.RGBA32;
            importer.SetPlatformTextureSettings(settings);
            changed = true;
        }
        if (changed) importer.SaveAndReimport();
    }
}
