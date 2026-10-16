using System;
using BH.SDK.Models.Device;
using BH.SDK.Rules;

namespace BH.SDK.Utils.Performance
{
    /// <summary> The devices the author measured the stress test on, standing in for a device that has not
    /// run it. </summary>
    public enum ReferenceDevice : byte
    {
        /// <summary> A weak Android phone. </summary>
        WeakAndroid = 0,

        /// <summary> A mid-range Android phone; iOS reads it until an iOS device is measured. </summary>
        MidAndroid = 1,

        /// <summary> A desktop PC with a discrete GPU. </summary>
        Pc = 2,

        /// <summary> A laptop on its integrated GPU. </summary>
        LaptopIGpu = 3,
    }

    // MidAndroid and Pc are stress test logs; WeakAndroid and LaptopIGpu are ESTIMATES scaled from them until
    // such a device is measured (Docs/PERFORMANCE_RATING.md, "Reference devices"; the client's
    // Docs/Issues/LEVEL_PERFORMANCE_HISTORY.md has the runs).

    /// <summary> The shipped profile of each reference device, the same constants the server scores with.
    /// </summary>
    public static class ReferenceDeviceProfiles
    {
        /// <summary> Every reference device, in enum order. </summary>
        public static readonly ReferenceDevice[] All =
        {
            ReferenceDevice.WeakAndroid, ReferenceDevice.MidAndroid, ReferenceDevice.Pc, ReferenceDevice.LaptopIGpu,
        };

        /// <summary> A fresh copy of the profile of <paramref name="device"/>; null for a value outside the
        /// enum. </summary>
        public static DeviceProfile For(ReferenceDevice device) => device switch
        {
            ReferenceDevice.WeakAndroid => WeakAndroid(),
            ReferenceDevice.MidAndroid => MidAndroid(),
            ReferenceDevice.Pc => Pc(),
            ReferenceDevice.LaptopIGpu => LaptopIGpu(),
            _ => null,
        };

        /// <summary> Whether <paramref name="device"/>'s profile is an estimate rather than a measurement.
        /// </summary>
        public static bool IsEstimate(ReferenceDevice device)
            => device is ReferenceDevice.WeakAndroid or ReferenceDevice.LaptopIGpu;

        private static DeviceProfile WeakAndroid() => new()
        {
            BenchmarkVersion = PerformanceRules.BenchmarkVersion,
            DeviceModel = "estimated: weak Android",
            GpuName = "estimated: entry-level mobile GPU",
            ScreenWidth = 1600,
            ScreenHeight = 720,
            GpuTiming = true,
            CpuBase = 23.31f,
            GpuBase = 9.612f,
            CpuCoefficients = new[]
            {
                0.000956f, 1.6e-05f, 6.2e-05f, 0.027846f, 0.04993f, 0.001534f,
                0.07094f, 0.004698f, 0.009404f, 0.01653f, 0f, 0.134292f,
            },
            GpuCoefficients = new[]
            {
                0.00063f, 0f, 0f, 0f, 0.020585f, 1.25e-05f,
                0.3192f, 0.0079975f, 0.006825f, 0.904566f, 0.261978f, 1.3803252f,
            },
            CpuFit = new[]
            {
                0.9586f, 0.9886f, 0.9927f, 0.9997f, 0.9995f, 0.9977f,
                0.9942f, 0.9998f, 0.9962f, 0.0823f, 0.0814f, 0.8312f,
            },
            GpuFit = new[]
            {
                0.8733f, 0.1907f, 0.0002f, 0.9995f, 0.9709f, 0.4376f,
                0.9585f, 0.9953f, 0.9731f, 0.9887f, 0.997f, 0.9918f,
            },
            AreaFactors = new[]
            {
                1f, 1.578997f, 3.162828f, 3.145656f, 1.086102f,
            },
            DriftError = 0.028f,
            Trusted = false,
            MeasuredAt = new DateTime(2026, 10, 16, 21, 55, 0, DateTimeKind.Utc),
        };

        private static DeviceProfile MidAndroid() => new()
        {
            BenchmarkVersion = PerformanceRules.BenchmarkVersion,
            DeviceModel = "Xiaomi 2209116AG",
            GpuName = "Adreno (TM) 618",
            ScreenWidth = 2400,
            ScreenHeight = 1080,
            GpuTiming = true,
            CpuBase = 11.655f,
            GpuBase = 8.01f,
            CpuCoefficients = new[]
            {
                0.000478f, 8e-06f, 3.1e-05f, 0.013923f, 0.024965f, 0.000767f,
                0.03547f, 0.002349f, 0.004702f, 0.008265f, 0f, 0.067146f,
            },
            GpuCoefficients = new[]
            {
                0.000252f, 0f, 0f, 0f, 0.008234f, 5e-06f,
                0.12768f, 0.003199f, 0.00273f, 0.753805f, 0.218315f, 1.150271f,
            },
            CpuFit = new[]
            {
                0.9586f, 0.9886f, 0.9927f, 0.9997f, 0.9995f, 0.9977f,
                0.9942f, 0.9998f, 0.9962f, 0.0823f, 0.0814f, 0.8312f,
            },
            GpuFit = new[]
            {
                0.8733f, 0.1907f, 0.0002f, 0.9995f, 0.9709f, 0.4376f,
                0.9585f, 0.9953f, 0.9731f, 0.9887f, 0.997f, 0.9918f,
            },
            AreaFactors = new[]
            {
                1f, 1.578997f, 3.162828f, 3.145656f, 1.086102f,
            },
            DriftError = 0.028f,
            Trusted = false,
            MeasuredAt = new DateTime(2026, 10, 16, 21, 55, 0, DateTimeKind.Utc),
        };

        private static DeviceProfile Pc() => new()
        {
            BenchmarkVersion = PerformanceRules.BenchmarkVersion,
            DeviceModel = "B650 EAGLE (Gigabyte Technology Co., Ltd.)",
            GpuName = "NVIDIA GeForce RTX 4060 Ti",
            ScreenWidth = 1920,
            ScreenHeight = 1080,
            GpuTiming = true,
            CpuBase = 1.54615f,
            GpuBase = 0.185344f,
            CpuCoefficients = new[]
            {
                6.897981e-05f, 3.595417e-06f, 0f, 0.00164337165f, 0.004002128f, 0.000173558423f,
                0f, 0.000302952161f, 0.00102888641f, 0f, 0f, 0.008913513f,
            },
            GpuCoefficients = new[]
            {
                4.10107219e-07f, 3.745814e-08f, 0f, 9.908144e-06f, 0f, 8.486966e-07f,
                0f, 2.13366384e-05f, 2.05694323e-05f, 0.02998204f, 0.009314167f, 0.0198849738f,
            },
            CpuFit = new[]
            {
                0.996466f, 0.9646632f, 0.185710266f, 0.9981869f, 0.9956677f, 0.9987282f,
                0.165914908f, 0.984214365f, 0.379143178f, 0.221540809f, 0.0103037655f, 0.666738f,
            },
            GpuFit = new[]
            {
                0.9950991f, 0.97073096f, 0.7795275f, 0.9069848f, 0.5596133f, 0.9239797f,
                0.6048177f, 0.995759666f, 0.9900187f, 0.9999734f, 0.9901011f, 0.97850436f,
            },
            AreaFactors = new[]
            {
                1f, 1.58730161f, 1.58730161f, 2.41269851f, 1.65079367f,
            },
            DriftError = 0.105489068f,
            Trusted = true,
            MeasuredAt = new DateTime(2026, 10, 16, 21, 17, 13, DateTimeKind.Utc),
        };

        private static DeviceProfile LaptopIGpu() => new()
        {
            BenchmarkVersion = PerformanceRules.BenchmarkVersion,
            DeviceModel = "estimated: laptop",
            GpuName = "estimated: integrated GPU",
            ScreenWidth = 1920,
            ScreenHeight = 1080,
            GpuTiming = true,
            CpuBase = 2.47384f,
            GpuBase = 1.482752f,
            CpuCoefficients = new[]
            {
                0.000110367696f, 5.7526672e-06f, 0f, 0.00262939464f, 0.0064034048f, 0.000277693477f,
                0f, 0.000484723458f, 0.00164621826f, 0f, 0f, 0.0142616208f,
            },
            GpuCoefficients = new[]
            {
                3.28085775e-06f, 2.9966512e-07f, 0f, 7.9265152e-05f, 0f, 6.7895728e-06f,
                0f, 0.000170693107f, 0.000164555458f, 0.23985632f, 0.074513336f, 0.15907979f,
            },
            CpuFit = new[]
            {
                0.996466f, 0.9646632f, 0.185710266f, 0.9981869f, 0.9956677f, 0.9987282f,
                0.165914908f, 0.984214365f, 0.379143178f, 0.221540809f, 0.0103037655f, 0.666738f,
            },
            GpuFit = new[]
            {
                0.9950991f, 0.97073096f, 0.7795275f, 0.9069848f, 0.5596133f, 0.9239797f,
                0.6048177f, 0.995759666f, 0.9900187f, 0.9999734f, 0.9901011f, 0.97850436f,
            },
            AreaFactors = new[]
            {
                1f, 1.9f, 2.8f, 4.2f, 1.4f,
            },
            DriftError = 0.105489068f,
            Trusted = false,
            MeasuredAt = new DateTime(2026, 10, 16, 21, 17, 13, DateTimeKind.Utc),
        };
    }
}
