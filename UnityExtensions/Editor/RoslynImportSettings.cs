using System;
using System.Linq;
using UnityEditor;

namespace BH.SDK.Editor
{
    // The import settings are the mechanism: a default .meta pushes Roslyn into every assembly. Called
    // explicitly by the tool that wrote the file, never from a hook; every setting is compared before it is
    // written (Docs/ROSLYN_GENERATORS.md).

    /// <summary> Applies the import settings the SDK's Roslyn assets require. </summary>
    internal static class RoslynImportSettings
    {
        /// <summary> Editor-only, referenced by name alone. Returns whether anything changed. </summary>
        public static bool ApplyCompilerReference(string assetPath)
        {
            if (AssetImporter.GetAtPath(assetPath) is not PluginImporter importer) return false;

            var changed = false;
            if (importer.GetCompatibleWithAnyPlatform())
            {
                importer.SetCompatibleWithAnyPlatform(false);
                changed = true;
            }
            if (!importer.GetCompatibleWithEditor())
            {
                importer.SetCompatibleWithEditor(true);
                changed = true;
            }
            changed |= SetExplicitlyReferenced(importer, true);

            if (changed) importer.SaveAndReimport();
            return changed;
        }

        /// <summary> No platform at all, plus the label. Returns whether anything changed. </summary>
        public static bool ApplyAnalyzer(string assetPath)
        {
            if (AssetImporter.GetAtPath(assetPath) is not PluginImporter importer) return false;

            var changed = false;
            if (importer.GetCompatibleWithAnyPlatform())
            {
                importer.SetCompatibleWithAnyPlatform(false);
                changed = true;
            }
            if (importer.GetCompatibleWithEditor())
            {
                importer.SetCompatibleWithEditor(false);
                changed = true;
            }

            foreach (var target in Enum.GetValues(typeof(BuildTarget)).Cast<BuildTarget>())
            {
                if (target <= 0) continue;
                try
                {
                    if (!importer.GetCompatibleWithPlatform(target)) continue;
                    importer.SetCompatibleWithPlatform(target, false);
                    changed = true;
                }
                catch (ArgumentException)
                {
                    // An obsolete BuildTarget this Editor no longer accepts. Nothing to disable.
                }
            }

            if (changed) importer.SaveAndReimport();
            return ApplyLabel(assetPath) || changed;
        }

        private static bool ApplyLabel(string assetPath)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset == null) return false;

            var labels = AssetDatabase.GetLabels(asset);
            if (labels.Contains(RoslynLayout.AnalyzerLabel)) return false;

            AssetDatabase.SetLabels(asset, labels.Append(RoslynLayout.AnalyzerLabel).ToArray());
            return true;
        }

        // "Auto Referenced" has no public API on PluginImporter, so it goes through SerializedObject.
        // The property name is checked rather than assumed - a rename in a future Editor must
        // degrade to "left auto-referenced", never to an exception during an import.
        private static bool SetExplicitlyReferenced(PluginImporter importer, bool value)
        {
            using var serialized = new SerializedObject(importer);
            var property = serialized.FindProperty("m_IsExplicitlyReferenced")
                           ?? serialized.FindProperty("isExplicitlyReferenced");
            if (property == null || property.boolValue == value) return false;

            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }
    }

    // The SAFETY NET, not the mechanism - see above. This catches what no tool was involved in: a
    // fresh clone whose Refs/ .meta files do not exist, a file dropped in by hand, a Reimport from
    // the Project view.

    /// <summary> Re-applies the settings whenever one of those assets is imported. </summary>
    internal sealed class RoslynImportPostprocessor : AssetPostprocessor
    {
        private const string ReferencesFolder = "/Roslyn/Refs/";

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted,
            string[] moved, string[] movedFrom)
        {
            var analyzerSuffix = "/" + RoslynLayout.ArtifactFileName;

            var paths = imported.Concat(moved)
                .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                .Where(path => path.Contains(ReferencesFolder, StringComparison.Ordinal)
                               || path.EndsWith(analyzerSuffix, StringComparison.Ordinal))
                .ToArray();
            if (paths.Length == 0) return;

            // Deferred, because re-importing from inside the import callback that is still running
            // is what turns one wrong setting into a stalled Editor.
            EditorApplication.delayCall += () =>
            {
                foreach (var path in paths)
                {
                    if (path.EndsWith(analyzerSuffix, StringComparison.Ordinal))
                        RoslynImportSettings.ApplyAnalyzer(path);
                    else
                        RoslynImportSettings.ApplyCompilerReference(path);
                }
            };
        }
    }
}
