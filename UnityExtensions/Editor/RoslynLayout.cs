using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace BH.SDK.Editor
{
    // Every path is derived from this assembly's own asmdef location; nothing may name the mount folder.
    // Deliberately not generic: one project, one artifact, one folder of references.

    /// <summary> Filesystem layout of the SDK's Roslyn components. </summary>
    internal static class RoslynLayout
    {
        // Used by the FALLBACK lookup only. It is the asmdef's own file name, which is not the same
        // string as the namespace or the rootNamespace - getting those two confused is exactly what
        // made the first version of this class report "cannot locate the SDK" from every menu item.

        /// <summary> File name (no extension) of the asmdef declaring this assembly. </summary>
        private const string SelfAsmdefFileName = "BH.SDK.UnityExtensions.Editor";

        /// <summary> Assembly name of the shipping analyzer. NOT the asmdef's - see the csproj. </summary>
        public const string ArtifactAssemblyName = "BH.SDK.Roslyn";

        /// <summary> The built analyzer's file name - the only thing Unity actually loads. </summary>
        public const string ArtifactFileName = ArtifactAssemblyName + ".dll";

        /// <summary> The project the build shells out to. </summary>
        public const string ProjectFileName = ArtifactAssemblyName + ".csproj";

        /// <summary> Asset label Unity reads to treat a .dll as an analyzer. Case sensitive. </summary>
        public const string AnalyzerLabel = "RoslynAnalyzer";

        /// <summary> Release, always: an analyzer runs on every compilation the Editor performs. </summary>
        public const string Configuration = "Release";

        /// <summary> The five assemblies the sources bind against; the last two exist because the Editor's
        /// System.Immutable spells members with Span (else CS0012 naming a type nobody wrote). </summary>
        public static readonly string[] CompilerReferenceNames =
        {
            "Microsoft.CodeAnalysis.dll",
            "Microsoft.CodeAnalysis.CSharp.dll",
            "System.Collections.Immutable.dll",
            "System.Memory.dll",
            "System.Runtime.CompilerServices.Unsafe.dll",
        };

        /// <summary> Project-relative root of the SDK, or null when it cannot be located. </summary>
        public static string SdkRootAssetPath => FromThisFile() ?? FromAsmdefSearch();

        // <sdk>/UnityExtensions/Editor/RoslynLayout.cs - up three folders to reach <sdk>.
        private static string FromThisFile()
        {
            var file = ThisFilePath()?.Replace('\\', '/');
            if (string.IsNullOrEmpty(file)) return null;

            const string assets = "/Assets/";
            var index = file.LastIndexOf(assets, StringComparison.Ordinal);
            if (index < 0) return null;

            var assetPath = file.Substring(index + 1);
            var root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(assetPath)));
            if (string.IsNullOrEmpty(root)) return null;

            root = root.Replace('\\', '/');
            return Directory.Exists(ToAbsolute(root)) ? root : null;
        }

        // The fallback, for the case the compiled-in path no longer resolves - the SDK moved inside
        // Assets/ since this assembly was built, say.
        private static string FromAsmdefSearch()
        {
            foreach (var guid in AssetDatabase.FindAssets($"{SelfAsmdefFileName} t:AssemblyDefinitionAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) != SelfAsmdefFileName) continue;

                var root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(path)));
                if (!string.IsNullOrEmpty(root)) return root.Replace('\\', '/');
            }
            return null;
        }

        /// <summary> This file's path, stamped in by the compiler. </summary>
        private static string ThisFilePath([CallerFilePath] string path = null) => path;

        /// <summary> Where the analyzer's SOURCES live - not the artifact, which sits a level up. </summary>
        public static string RoslynAssetPath => $"{SdkRootAssetPath}/Roslyn";

        /// <summary> The compiler references, installed rather than committed. </summary>
        public static string ReferencesAssetPath => $"{RoslynAssetPath}/Refs";

        /// <summary> The project file, as an asset path. </summary>
        public static string ProjectFileAssetPath => $"{RoslynAssetPath}/{ProjectFileName}";

        // Unity hands an analyzer to the asmdef owning its folder and to every assembly referencing
        // that asmdef - measured, not assumed. Inside Roslyn/ it would belong to
        // BH.SDK.Roslyn.Sources, which nothing references, and would analyze nothing at all.

        /// <summary> The artifact lives in the SDK ROOT, and that is what gives it its scope. </summary>
        public static string ArtifactAssetPath => $"{SdkRootAssetPath}/{ArtifactFileName}";

        /// <summary> MSBuild output, behind a tilde so Unity never imports it. </summary>
        public static string BuiltArtifactPath
            => $"{ToAbsolute(RoslynAssetPath)}/bin~/{Configuration}/{ArtifactFileName}";

        /// <summary> The Editor's own Roslyn - by definition the version its compiler binds against. </summary>
        public static string EditorCompilerReferenceDirectory
            => $"{EditorApplication.applicationContentsPath}/Tools/BuildPipeline/Unity.Analyzers.Common";

        /// <summary> The .NET SDK Unity ships, so nothing has to be installed separately. </summary>
        public static string BundledDotnetPath
            => $"{EditorApplication.applicationContentsPath}/DotNetSdk/dotnet.exe";

        /// <summary> The folder holding Assets, with forward slashes throughout. </summary>
        public static string ProjectRoot => Path.GetDirectoryName(Application.dataPath)!.Replace('\\', '/');

        /// <summary> An asset path as one the dotnet build can open. </summary>
        public static string ToAbsolute(string assetPath) => $"{ProjectRoot}/{assetPath}";
    }
}
