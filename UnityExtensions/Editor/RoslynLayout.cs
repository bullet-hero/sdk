using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace BH.SDK.Editor
{
    // WHERE EVERYTHING IS, ASKED RATHER THAN WRITTEN DOWN. The SDK is a submodule; the folder a
    // consuming project mounts it at is that project's business, so nothing here may name it. The
    // one fixed point is THIS assembly: Unity can hand back the path of the asmdef that declares
    // it, and the SDK root is two folders above that. Every other path is derived from it.
    //
    // DELIBERATELY NOT GENERIC. This is the builder for BH.SDK.Roslyn and nothing else - one
    // project, one artifact, one folder of compiler references. A configurable version of this
    // would have to describe things that have exactly one correct value.

    /// <summary> Filesystem layout of the SDK's Roslyn components. </summary>
    internal static class RoslynLayout
    {
        /// <summary> File name (no extension) of the asmdef declaring this assembly. </summary>
        // Used by the FALLBACK lookup only. It is the asmdef's own file name, which is not the same
        // string as the namespace or the rootNamespace - getting those two confused is exactly what
        // made the first version of this class report "cannot locate the SDK" from every menu item.
        private const string SelfAsmdefFileName = "BH.SDK.UnityExtensions.Editor";

        /// <summary> Assembly name of the shipping analyzer. NOT the asmdef's - see the csproj. </summary>
        public const string ArtifactAssemblyName = "BH.SDK.Roslyn";

        public const string ArtifactFileName = ArtifactAssemblyName + ".dll";
        public const string ProjectFileName = ArtifactAssemblyName + ".csproj";

        /// <summary> Asset label Unity reads to treat a .dll as an analyzer. Case sensitive. </summary>
        public const string AnalyzerLabel = "RoslynAnalyzer";

        /// <summary> Release, always: an analyzer runs on every compilation the Editor performs. </summary>
        public const string Configuration = "Release";

        /// <summary>
        /// The five assemblies the sources bind against. Three are an analyzer's own surface
        /// (Roslyn plus ImmutableArray); the last two are there because the Editor's
        /// System.Collections.Immutable spells its members with Span, and without them the build
        /// fails with CS0012 naming a type nobody wrote.
        /// </summary>
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

        public static string RoslynAssetPath => $"{SdkRootAssetPath}/Roslyn";
        public static string ReferencesAssetPath => $"{RoslynAssetPath}/Refs";
        public static string ProjectFileAssetPath => $"{RoslynAssetPath}/{ProjectFileName}";

        /// <summary> The artifact lives in the SDK ROOT, and that is what gives it its scope. </summary>
        // Unity hands an analyzer to the asmdef owning its folder and to every assembly referencing
        // that asmdef - measured, not assumed. Inside Roslyn/ it would belong to
        // BH.SDK.Roslyn.Sources, which nothing references, and would analyze nothing at all.
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

        public static string ProjectRoot => Path.GetDirectoryName(Application.dataPath)!.Replace('\\', '/');

        public static string ToAbsolute(string assetPath) => $"{ProjectRoot}/{assetPath}";
    }
}
