using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace BH.SDK.Roslyn.Tests
{
    // A generator is a pure function from a compilation to sources, so testing one needs a
    // compilation and nothing else - no Unity, no disk, no project. This builds one from strings.
    //
    // THE MODEL API IS STUBBED HERE RATHER THAN REFERENCED FROM BH.SDK, and the reason is when
    // these tests have to work: the mass refactor that puts `partial`/`sealed`/[GenerateModel] on
    // 205 files leaves the library uncompilable for long stretches, which is exactly when a broken
    // generator has to be visible. A stub keeps that signal alive. The cost is drift, and it is
    // paid for on the other side: BH.SDK.Tests exercises the generator against the REAL types.

    internal static class GeneratorHarness
    {
        /// <summary> The subset of the SDK's model API a generated model is written against. </summary>
        public const string ModelApiStub = @"
namespace System.Runtime.CompilerServices { internal static class IsExternalInit {} }

namespace Newtonsoft.Json
{
    [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field)]
    public sealed class JsonPropertyAttribute : System.Attribute
    {
        public JsonPropertyAttribute(string propertyName) { PropertyName = propertyName; }
        public string PropertyName { get; }
    }

    [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field)]
    public sealed class JsonIgnoreAttribute : System.Attribute { }
}

namespace BH.SDK.Models.Interfaces
{
    public interface ICopyable<out T> : System.ICloneable { T Copy(); }
    public interface IResetable { void Reset(); }
    public interface IUpdatable<in T> { void Update(T src); }
    public interface IMoveable<in T> { void Pull(T source); }
    public interface IModel<T> : ICopyable<T>, System.IEquatable<T>, IResetable, IUpdatable<T>, IMoveable<T> { }
}

namespace BH.SDK.Models.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
    public sealed class GenerateModelAttribute : System.Attribute { }

    [System.AttributeUsage(System.AttributeTargets.Property)]
    public sealed class GenerateModelIgnoreAttribute : System.Attribute { }

    [System.AttributeUsage(System.AttributeTargets.Property)]
    public sealed class GenerateModelMergeAttribute : System.Attribute { }
}

namespace BH.SDK.Utils
{
    using System.Collections.Generic;
    using System.Linq;
    using BH.SDK.Models.Interfaces;

    // The real ModelUtils, cut down to the members generated code calls. Same signatures and same
    // constraints - the constraints are half of what these tests check, since a wrong dictionary
    // overload is a compile error rather than a wrong answer.
    public static class ModelUtils
    {
        public static T[] CopyArray<T>(this T[] array) where T : class, ICopyable<T>
        {
            var copy = new T[array.Length];
            for (var i = 0; i < array.Length; i++) copy[i] = array[i]?.Copy();
            return copy;
        }
        public static T[] CopyArrayUnmanaged<T>(this T[] array) where T : unmanaged
        {
            var copy = new T[array.Length];
            array.CopyTo(copy, 0);
            return copy;
        }
        public static List<T> CopyList<T>(this List<T> list) where T : ICopyable<T>
            => list.Select(i => i.Copy()).ToList();
        public static Dictionary<TKey, TValue> CopyDictionary<TKey, TValue>(this Dictionary<TKey, TValue> d)
            where TKey : unmanaged where TValue : ICopyable<TValue>
            => d.ToDictionary(p => p.Key, p => p.Value.Copy());
        public static Dictionary<TKey, TValue> CopyDictionaryManaged<TKey, TValue>(this Dictionary<TKey, TValue> d)
            where TKey : ICopyable<TKey> where TValue : ICopyable<TValue>
            => d.ToDictionary(p => p.Key.Copy(), p => p.Value.Copy());
        public static T PullFrom<T>(this T target, T source) where T : class, IModel<T>
        {
            if (source is null) return null;
            if (target is null || target.GetType() != source.GetType()) return source.Copy();
            target.Pull(source);
            return target;
        }
        public static void PullDictionary<TKey, TValue>(this Dictionary<TKey, TValue> target,
            Dictionary<TKey, TValue> source, System.Func<TValue, TValue, TValue> pullValue) { }
        public static void PullDictionary<TKey, TValue>(this Dictionary<TKey, TValue> target,
            Dictionary<TKey, TValue> source) where TValue : class, IModel<TValue> { }
        public static List<T> ResetTo<T>(this List<T> target, List<T> defaults)
        {
            if (target is null) return defaults;
            target.Clear();
            if (defaults != null) target.AddRange(defaults);
            return target;
        }
        public static Dictionary<TKey, TValue> ResetTo<TKey, TValue>(
            this Dictionary<TKey, TValue> target, Dictionary<TKey, TValue> defaults)
        {
            if (target is null) return defaults;
            target.Clear();
            if (defaults != null)
                foreach (var pair in defaults) target[pair.Key] = pair.Value;
            return target;
        }
        public static T[] ResetTo<T>(this T[] target, T[] defaults)
        {
            if (target is null || defaults is null || target.Length != defaults.Length) return defaults;
            defaults.CopyTo(target, 0);
            return target;
        }
        public static bool ArrayEquals<T>(this T[] a, T[] b) => a != null && b != null && a.SequenceEqual(b);
        public static bool ListEquals<T>(this List<T> a, List<T> b) => a != null && b != null && a.SequenceEqual(b);
        public static bool DictionaryEquals<TKey, TValue>(this Dictionary<TKey, TValue> a, Dictionary<TKey, TValue> b)
            => a != null && b != null && a.Count == b.Count;
        public static int GetArrayHashCode<T>(this T[] a) => a?.Length ?? 0;
        public static int GetListHashCode<T>(this List<T> a) => a?.Count ?? 0;
        public static int GetDictionaryHashCode<TKey, TValue>(this Dictionary<TKey, TValue> a) => a?.Count ?? 0;
    }
}

namespace BH.SDK.Serialization.Blob
{
    using System;

    // The blob runtime, cut to what generated code calls. Same signatures as the real one: a
    // ref struct passed by ref, so a stub that got that wrong would not even compile the emission.
    public sealed class BlobFormatException : Exception
    {
        public BlobFormatException(string message) : base(message) { }
    }

    public ref struct BlobWriter
    {
        public const int NullLength = -1;
        public int Length => 0;
        public void WriteByte(byte v) { }
        public void WriteBool(bool v) { }
        public void WriteShort(short v) { }
        public void WriteUShort(ushort v) { }
        public void WriteInt(int v) { }
        public void WriteUInt(uint v) { }
        public void WriteLong(long v) { }
        public void WriteULong(ulong v) { }
        public void WriteFloat(float v) { }
        public void WriteDouble(double v) { }
        public void WriteDateTime(DateTime v) { }
        public void WriteGuid(Guid v) { }
        public void WriteString(string v) { }
        public void PatchInt(int position, int value) { }
        public int ReserveInt() => 0;
    }

    public ref struct BlobReader
    {
        public int Remaining => 0;
        public int Position => 0;
        public byte ReadByte() => 0;
        public bool ReadBool() => false;
        public short ReadShort() => 0;
        public ushort ReadUShort() => 0;
        public int ReadInt() => 0;
        public uint ReadUInt() => 0;
        public long ReadLong() => 0;
        public ulong ReadULong() => 0;
        public float ReadFloat() => 0;
        public double ReadDouble() => 0;
        public DateTime ReadDateTime() => default;
        public Guid ReadGuid() => default;
        public string ReadString() => null;
        public int ReadCount(int stride) => 0;
    }

    public interface IBinaryModel
    {
        void Write(ref BlobWriter writer);
        void Read(ref BlobReader reader);
    }

    public static class BlobModels
    {
        public static T Read<T>(ref BlobReader reader) where T : class, IBinaryModel, new() => null;
    }

    public static class BlobVersions
    {
        public static Version Read(ref BlobReader reader) => null;
    }

    public static class BlobPrimitives
    {
    }
}

namespace BH.SDK.Serialization.Json
{
    using System;
    using Newtonsoft.Json;

    // The json runtime, cut to what generated code calls.
    public interface IJsonModel
    {
        void WriteJson(JsonWriter writer);
        void ReadJson(JsonReader reader);
        bool ReadJsonMember(JsonReader reader, string name);
    }

    public static class JsonModels
    {
        public static void ReadObject(JsonReader reader, IJsonModel model) { }
        public static void WriteEnvelope(JsonWriter writer, IJsonModel value, string version) { }
        public static T Read<T>(JsonReader reader) where T : class, IJsonModel, new() => null;
        public static T ReadEnveloped<T>(JsonReader reader) where T : class, IJsonModel, new() => null;
    }

    public static class JsonPrimitives
    {
        public static Guid ReadGuid(JsonReader reader) => default;
        public static DateTime ReadDateTime(JsonReader reader) => default;
        public static void WriteVersion(JsonWriter writer, Version value) { }
        public static Version ReadVersion(JsonReader reader) => null;
    }
}

namespace BH.SDK.Models
{
    public static class Names
    {
        public const string Frame = ""f"";
        public const string Value = ""v"";
        public const string Name = ""name"";
        public const string Layer = ""layer"";
    }
}
";

        /// <summary>
        /// Compiles <paramref name="sources"/> plus the stub API, runs <paramref name="generators"/>
        /// over the result, and hands back everything a test might assert on.
        /// </summary>
        public static GeneratorRun Run(IEnumerable<IIncrementalGenerator> generators,
            params string[] sources)
        {
            var compilation = CreateCompilation(sources);

            var driver = CSharpGeneratorDriver
                .Create(generators.Select(GeneratorExtensions.AsSourceGenerator).ToArray(),
                    parseOptions: ParseOptions,
                    driverOptions: new GeneratorDriverOptions(
                        IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true))
                .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

            return new GeneratorRun(driver, output, diagnostics);
        }

        /// <summary> Same, run a second time against an edited compilation, for incrementality. </summary>
        public static GeneratorRun RunAgain(GeneratorRun first, params string[] sources)
        {
            var compilation = CreateCompilation(sources);
            var driver = first.Driver
                .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

            return new GeneratorRun(driver, output, diagnostics);
        }

        public static CSharpParseOptions ParseOptions { get; } =
            new CSharpParseOptions(LanguageVersion.CSharp9);

        private static CSharpCompilation CreateCompilation(IEnumerable<string> sources)
        {
            var trees = new List<Microsoft.CodeAnalysis.SyntaxTree>
            {
                CSharpSyntaxTree.ParseText(ModelApiStub, ParseOptions),
            };
            trees.AddRange(sources.Select(source => CSharpSyntaxTree.ParseText(source, ParseOptions)));

            return CSharpCompilation.Create("BH.SDK.Generated.TestAssembly", trees, ReferenceSet,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        // Every assembly the runtime already loaded, which on net8 is the reference set plus the
        // BCL. Enumerating the trusted platform list rather than naming assemblies keeps this from
        // failing on a machine with a different SDK patch installed.
        private static ImmutableArray<MetadataReference> ReferenceSet { get; } = BuildReferenceSet();

        private static ImmutableArray<MetadataReference> BuildReferenceSet()
        {
            var trusted = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
            var builder = ImmutableArray.CreateBuilder<MetadataReference>();

            foreach (var path in trusted.Split(Path.PathSeparator))
            {
                if (path.Length == 0) continue;
                builder.Add(MetadataReference.CreateFromFile(path));
            }

            return builder.ToImmutable();
        }
    }

    /// <summary> One driver run, plus the pieces a test asserts on. </summary>
    internal readonly struct GeneratorRun
    {
        public GeneratorRun(GeneratorDriver driver, Compilation output,
            ImmutableArray<Diagnostic> driverDiagnostics)
        {
            Driver = driver;
            Output = output;
            DriverDiagnostics = driverDiagnostics;
        }

        public GeneratorDriver Driver { get; }
        public Compilation Output { get; }
        public ImmutableArray<Diagnostic> DriverDiagnostics { get; }

        public GeneratorDriverRunResult Result => Driver.GetRunResult();

        /// <summary> Sources the generators added, keyed by hint name. </summary>
        public IReadOnlyDictionary<string, string> Sources => Result.Results
            .SelectMany(result => result.GeneratedSources)
            .ToDictionary(source => source.HintName, source => source.SourceText.ToString());

        /// <summary> Diagnostics the generators reported. </summary>
        public ImmutableArray<Diagnostic> GeneratorDiagnostics => Result.Diagnostics;

        /// <summary> Errors from compiling the ORIGINAL sources together with the generated ones. </summary>
        public ImmutableArray<Diagnostic> CompilationErrors => Output
            .GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();

        public string Source(string hintName) => Sources.TryGetValue(hintName, out var source)
            ? source
            : throw new KeyNotFoundException(
                $"No generated source '{hintName}'. Generated: {string.Join(", ", Sources.Keys)}");
    }
}
