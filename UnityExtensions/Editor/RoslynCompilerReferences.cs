using System.IO;
using UnityEditor;

namespace BH.SDK.Editor
{
    // WHY THESE ARE INSTALLED RATHER THAN COMMITTED. BH.SDK.Roslyn.Sources.asmdef compiles the
    // analyzer sources so the IDE can resolve them, and for that Unity needs the Roslyn assemblies
    // as real plugin assets inside Assets/. Committing them would put 13 MB of Microsoft binaries
    // into an open-source SDK repo AND pin a Roslyn version into git that has to follow whichever
    // Editor is open - so they are copied out of the Editor's own installation instead.
    //
    // IT RUNS ON LOAD, not from a menu item alone, because a fresh clone has no Refs/ and would
    // otherwise open with an asmdef that cannot resolve its references. The check is five
    // File.Exists calls, so it costs nothing on the loads where it finds them.

    /// <summary> Installs this Editor's Roslyn assemblies into the SDK's Roslyn/Refs/ folder. </summary>
    [InitializeOnLoad]
    internal static class RoslynCompilerReferences
    {
        static RoslynCompilerReferences()
        {
            if (RoslynLayout.SdkRootAssetPath == null) return;
            if (IsInstalled()) return;
            EditorApplication.delayCall += () => Install(false);
        }

        [MenuItem("Tools/BH.SDK.Roslyn/Install Compiler References", priority = 100)]
        private static void InstallMenu() => Install(true);

        /// <summary> True when every reference the asmdef names is already present. </summary>
        public static bool IsInstalled()
        {
            var refs = RoslynLayout.ReferencesAssetPath;
            foreach (var name in RoslynLayout.CompilerReferenceNames)
            {
                if (!File.Exists(RoslynLayout.ToAbsolute($"{refs}/{name}")))
                    return false;
            }
            return true;
        }

        /// <summary> Copies the compiler assemblies out of this Editor's installation. </summary>
        public static bool Install(bool verbose)
        {
            if (RoslynLayout.SdkRootAssetPath == null)
            {
                Cat.MeowError("[Roslyn] Cannot locate the SDK root from this assembly's own file path.");
                return false;
            }

            var source = RoslynLayout.EditorCompilerReferenceDirectory;
            if (!Directory.Exists(source))
            {
                Cat.MeowError($"[Roslyn] The Editor carries no Roslyn assemblies at '{source}'.");
                return false;
            }

            var targetAssetPath = RoslynLayout.ReferencesAssetPath;
            var target = RoslynLayout.ToAbsolute(targetAssetPath);
            Directory.CreateDirectory(target);

            foreach (var name in RoslynLayout.CompilerReferenceNames)
            {
                var from = $"{source}/{name}";
                if (!File.Exists(from))
                {
                    Cat.MeowError($"[Roslyn] '{name}' is missing from '{source}'.");
                    return false;
                }
                File.Copy(from, $"{target}/{name}", true);
            }

            AssetDatabase.ImportAsset(targetAssetPath, ImportAssetOptions.ImportRecursive);

            // Applied here rather than left to the postprocessor: this method knows the files just
            // changed, and Unity does not always re-run an import when only content did.
            foreach (var name in RoslynLayout.CompilerReferenceNames)
                RoslynImportSettings.ApplyCompilerReference($"{targetAssetPath}/{name}");

            if (verbose)
                Cat.Meow($"[Roslyn] Installed {RoslynLayout.CompilerReferenceNames.Length} compiler references into '{targetAssetPath}'.");
            return true;
        }
    }
}
