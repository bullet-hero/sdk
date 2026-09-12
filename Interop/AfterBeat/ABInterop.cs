using System.Collections.Generic;
using BH.SDK.Interop.AfterBeat.Export;
using BH.SDK.Interop.AfterBeat.Import;
using BH.SDK.Interop.AfterBeat.Models;
using BH.SDK.Models;
using BH.SDK.Models.Data;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;

namespace BH.SDK.Interop.AfterBeat
{
    // One entry point per THING a host actually converts, so nothing outside this folder has to
    // know which of the importers and maps to call in which order, or that a lone .vgp needs a
    // scope built for it while a level's prefabs do not.
    //
    // Everything here takes and returns TEXT, never a path: this LAYER reads no files, whatever the
    // rest of the SDK does (Services/Content addresses directories and archives). Where the
    // documents come from, what encoding they were in, and which of them the folder actually had
    // are all the host's business.

    /// <summary> Every Afterbeat conversion this library offers, in one place. </summary>
    public static class ABInterop
    {
        #region Levels

        /// <summary> A .vgd and its metadata into a level. </summary>
        public static ABLevelImporter.Result ImportLevel(string levelJson, string metaJson,
            ABOptions options = null)
            => ABLevelImporter.ImportJson(levelJson, metaJson, options);

        /// <summary> A level back into the two documents, as text ready to write. </summary>
        public static ExportedLevel ExportLevel(Level level, LevelMeta meta, ABOptions options = null)
        {
            var result = ABLevelExporter.Export(level, meta, options);
            return new ExportedLevel(
                result.Level == null ? null : ABSerialization.Serialize(result.Level),
                result.Meta == null ? null : ABSerialization.Serialize(result.Meta),
                result.Report);
        }

        /// <summary> The text of one exported level folder's two documents. </summary>
        public readonly struct ExportedLevel
        {
            /// <summary> Contents of level.vgd, or null when the export failed. </summary>
            public string LevelJson { get; }

            /// <summary> Contents of the metadata document. </summary>
            public string MetaJson { get; }

            /// <summary> What the conversion had to say about itself. </summary>
            public InteropReport Report { get; }

            /// <summary> Both documents plus the report, which is everything an export produces. </summary>
            public ExportedLevel(string levelJson, string metaJson, InteropReport report)
            {
                LevelJson = levelJson;
                MetaJson = metaJson;
                Report = report;
            }
        }

        #endregion

        #region Themes

        /// <summary> A standalone .vgt into a theme. </summary>
        public static ThemeData ImportTheme(string themeJson, InteropReport report = null)
        {
            report ??= new InteropReport();
            if (!ABSerialization.TryDeserialize<VgtTheme>(themeJson, out var source, out var error))
            {
                report.Failed("theme_unreadable", $"The .vgt could not be read: {error}", "theme");
                return null;
            }

            return ABThemeMap.Import(source, report, "theme");
        }

        /// <summary> A theme into a standalone .vgt. </summary>
        public static string ExportTheme(ThemeData theme, InteropReport report = null)
        {
            // A standalone .vgt carries no id of its own - it gets one when it is pasted into a
            // level. Writing the Guid anyway is harmless and makes a re-import land on the same
            // theme, which is what an author moving one file back and forth expects.
            var exported = ABThemeMap.Export(theme, theme?.ThemeId.value.ToString("N"), report, "theme");
            return ABSerialization.Serialize(exported);
        }

        #endregion

        #region Prefabs

        /// <summary> A standalone .vgp into a prefab template. Level shapes it needs to synthesize
        /// go into <paramref name="shapes"/>, which is usually the receiving level's own
        /// CompositeShapes so the prefab keeps working once it is placed. </summary>
        public static Prefab ImportPrefab(string prefabJson, ABOptions options = null,
            InteropReport report = null, IDictionary<ShapeId, CompositeShape> shapes = null,
            ThemeData referenceTheme = null, IDictionary<EffectId, EffectData> effects = null)
        {
            report ??= new InteropReport();
            options = (options ?? new ABOptions()).Sanitized();

            if (!ABSerialization.TryDeserialize<VgpPrefab>(prefabJson, out var source, out var error))
            {
                report.Failed("prefab_unreadable", $"The .vgp could not be read: {error}", "prefab");
                return null;
            }

            return ABPrefabImporter.ImportTemplate(source, options, report, shapes, referenceTheme,
                "prefab", effects);
        }

        /// <summary> A prefab template into a standalone .vgp. </summary>
        public static string ExportPrefab(Prefab prefab, ABOptions options = null,
            InteropReport report = null, ThemeData referenceTheme = null)
        {
            report ??= new InteropReport();
            options = (options ?? new ABOptions()).Sanitized();

            var context = new ABExportContext(options, report, prefab)
            {
                ReferenceTheme = referenceTheme,
            };

            var exported = new VgpPrefab
            {
                Id = prefab?.PrefabId.value.ToString("N") ?? string.Empty,
                Name = prefab?.Root?.Name ?? string.Empty,
                Type = (int)ABPrefabType.Misc1,
                Objects = ABObjectExporter.ExportAll(context, "objs"),
            };

            return ABSerialization.Serialize(exported);
        }

        #endregion
    }
}
