using System;
using System.IO;
using BH.SDK.Models;
using BH.SDK.Models.Values;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Blob;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Utils;
using BH.SDK.Versions;
using Newtonsoft.Json.Linq;

namespace BH.SDK.Samples.ConsoleSmoke
{
    // THE SDK AS A THIRD PARTY SEES IT: a DLL, a level folder, nothing else. Reads level + metadata,
    // prints what a tool would want to know, and round-trips the level through both formats. Any
    // mismatch - or a file this build refuses as newer - is a non-zero exit, so a script can run it.
    //
    // Exit codes: 0 ok, 1 usage, 2 missing file, 3 round-trip mismatch, 4 newer than this SDK.
    internal static class Program
    {
        private static readonly SerializationService Service = new();

        private static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("usage: ConsoleSmoke <level folder>");
                return 1;
            }

            var folder = args[0];
            Console.WriteLine($"BH.SDK {SdkVersion.Value}, model generation {ModelGenerations.Current}");

            try
            {
                var level = Read<Level>(folder, FileNames.LevelFileBaseName, out var levelFormat, out var levelGeneration);
                var meta = Read<LevelMeta>(folder, FileNames.MetadataFileBaseName, out _, out _);
                if (level == null || meta == null) return 2;

                Console.WriteLine($"name:       {NameOf(meta)}");
                Console.WriteLine($"objects:    {level.Game?.Objects?.Count ?? 0}");
                Console.WriteLine($"generation: {levelGeneration} ({levelFormat})");

                var ok = RoundTrip(level, SerializationType.Json) & RoundTrip(level, SerializationType.Blob);
                return ok ? 0 : 3;
            }
            catch (NewerGenerationException e)
            {
                Console.Error.WriteLine($"refused: {e.Message} (domain {e.Domain}, file {e.FileGeneration}, " +
                                        $"this SDK {e.BuildGeneration}) - update the SDK");
                return 4;
            }
        }

        // Resolving a localized name for a language is the host's business (the game has its own
        // rules for it); a plain one is printed as it is, a localized one as its first text.
        private static string NameOf(LevelMeta meta) => meta.LevelName switch
        {
            StringValue plain => plain.Value,
            StringLocalized localized when localized.Strings?.Count > 0 => localized.Strings[0].Value,
            _ => string.Empty,
        };

        private static T Read<T>(string folder, string baseName, out SerializationType format, out int generation)
            where T : class
        {
            foreach (SerializationType candidate in Enum.GetValues(typeof(SerializationType)))
            {
                var path = Path.Combine(folder, baseName + candidate.ToFileExtension());
                if (!File.Exists(path)) continue;

                var bytes = File.ReadAllBytes(path);
                format = candidate;
                generation = GenerationOf(bytes, candidate);
                return Service.DeserializeEnvelope<T>(bytes, candidate);
            }

            Console.Error.WriteLine($"no {baseName}.* in {folder}");
            format = SerializationType.Json;
            generation = ModelGenerations.Invalid;
            return null;
        }

        // The generation the FILE claims, read off its outermost envelope - the reader itself answers
        // with the build's own once it has migrated, so it cannot say.
        private static int GenerationOf(byte[] bytes, SerializationType format)
        {
            if (format == SerializationType.Blob)
            {
                var reader = new BlobReader(bytes, BlobFormat.HeaderLength, bytes.Length - BlobFormat.HeaderLength);
                reader.ReadString();
                return reader.ReadInt();
            }

            var text = System.Text.Encoding.UTF8.GetString(bytes).TrimStart('﻿');
            return JObject.Parse(text).Value<int?>(Names.Generation) ?? ModelGenerations.Invalid;
        }

        private static bool RoundTrip(Level level, SerializationType format)
        {
            var bytes = Service.SerializeEnvelope(level, format);
            var back = Service.DeserializeEnvelope<Level>(bytes, format);
            var equal = level.Equals(back);

            Console.WriteLine($"round trip {format}: {(equal ? "equal" : "MISMATCH")} ({bytes.Length} bytes)");
            return equal;
        }
    }
}
