using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;

namespace BH.SDK.Editor
{
    // THE ONE BUTTON THAT TURNS THE SOURCES INTO A WORKING ANALYZER. It shells out to MSBuild
    // rather than compiling in-process so the artifact is identical whether it was produced here or
    // by `dotnet build` on a machine with no Unity - one project file, one compiler, two callers.
    // The equivalent by hand is exactly:
    //
    //     cd <sdk>/Roslyn
    //     dotnet build BH.SDK.Roslyn.csproj -c Release
    //     cp bin~/Release/BH.SDK.Roslyn.dll ../BH.SDK.Roslyn.dll
    //
    // and the copy is not optional: bin~ is invisible to Unity, so a build left there changes
    // nothing. What this adds over the two commands is the import and its settings.
    //
    // THE dotnet IT RUNS IS UNITY'S OWN (Editor/Data/DotNetSdk/dotnet.exe), so nothing has to be
    // installed on a fresh machine and the SDK version follows the Editor.
    //
    // UNITY RECOMPILES THE WHOLE PROJECT AFTERWARDS. That is not overhead to optimize away - the
    // analyzer is an input to every assembly it is scoped to, and that is every assembly built on
    // BH.SDK.

    /// <summary> Builds BH.SDK.Roslyn and installs it as the SDK's analyzer. </summary>
    internal static class RoslynBuilder
    {
        [MenuItem("Tools/BH.SDK.Roslyn/Build Analyzer", priority = 0)]
        private static void BuildMenu() => Build();

        [MenuItem("Tools/BH.SDK.Roslyn/Open Roslyn Folder", priority = 20)]
        private static void OpenFolder()
        {
            if (RoslynLayout.SdkRootAssetPath == null) return;
            EditorUtility.RevealInFinder(RoslynLayout.ToAbsolute(RoslynLayout.RoslynAssetPath));
        }

        /// <summary> Compiles the csproj and installs its output into the SDK root. </summary>
        public static bool Build()
        {
            if (RoslynLayout.SdkRootAssetPath == null)
            {
                Cat.MeowError("[Roslyn] Cannot locate the SDK root from this assembly's own file path.");
                return false;
            }

            var projectFile = RoslynLayout.ToAbsolute(RoslynLayout.ProjectFileAssetPath);
            if (!File.Exists(projectFile))
            {
                Cat.MeowError($"[Roslyn] '{RoslynLayout.ProjectFileName}' not found at '{projectFile}'.");
                return false;
            }

            var dotnet = RoslynLayout.BundledDotnetPath;
            if (!File.Exists(dotnet))
            {
                Cat.MeowError($"[Roslyn] The Editor ships no dotnet CLI at '{dotnet}'.");
                return false;
            }

            RoslynCompilerReferences.Install(false);

            try
            {
                EditorUtility.DisplayProgressBar("Roslyn", $"Building {RoslynLayout.ArtifactAssemblyName}...", 0.5f);
                if (!Run(dotnet, projectFile, out var output))
                {
                    Cat.MeowError($"[Roslyn] Build failed.\n{output}");
                    return false;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            var built = RoslynLayout.BuiltArtifactPath;
            if (!File.Exists(built))
            {
                Cat.MeowError($"[Roslyn] Build reported success but produced no '{built}'.");
                return false;
            }

            try
            {
                File.Copy(built, RoslynLayout.ToAbsolute(RoslynLayout.ArtifactAssetPath), true);
            }
            catch (IOException exception)
            {
                // The compiler holds the analyzer open while it is loaded. Nothing here can force
                // that release, and reporting it beats leaving a half-updated artifact behind.
                Cat.MeowError("[Roslyn] Could not replace the analyzer - it is in use. " +
                              $"Close other Editors on this project and retry.\n{exception.Message}");
                return false;
            }

            AssetDatabase.ImportAsset(RoslynLayout.ArtifactAssetPath, ImportAssetOptions.ForceUpdate);
            RoslynImportSettings.ApplyAnalyzer(RoslynLayout.ArtifactAssetPath);

            Cat.Meow($"[Roslyn] Built and imported '{RoslynLayout.ArtifactAssetPath}'. " +
                     "Unity will recompile every assembly the analyzer is scoped to.");
            return true;
        }

        private static bool Run(string dotnet, string projectFile, out string output)
        {
            var startInfo = new ProcessStartInfo(dotnet)
            {
                Arguments = $"build \"{projectFile}\" -c {RoslynLayout.Configuration} -v minimal --nologo",
                WorkingDirectory = Path.GetDirectoryName(projectFile)!,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            // MSBuild resolves its SDK from the dotnet it is launched with, unless an inherited
            // MSBUILD*/DOTNET_* environment says otherwise. Clearing the two that matter keeps this
            // deterministic no matter what launched the Editor.
            startInfo.EnvironmentVariables.Remove("MSBuildSDKsPath");
            startInfo.EnvironmentVariables.Remove("MSBuildExtensionsPath");

            var text = new StringBuilder();
            using var process = new Process { StartInfo = startInfo };
            process.Start();
            text.Append(process.StandardOutput.ReadToEnd());
            text.Append(process.StandardError.ReadToEnd());
            process.WaitForExit();

            output = text.ToString().Trim();
            return process.ExitCode == 0;
        }
    }
}
