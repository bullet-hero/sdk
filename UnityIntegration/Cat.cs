using System;
#if BHSDK_UNITY
using UnityEngine;
#endif

namespace BH.SDK
{
    /// <summary> Logging that works with and without an engine: Unity's console under BHSDK_UNITY, the process
    /// console otherwise. The SDK's only logger, since it must also run as a plain library. </summary>
    public static class Cat
    {
        /// <summary> An ordinary line. </summary>
        public static void Meow(object message)
        {
#if BHSDK_UNITY
            Debug.Log(message);
#else
            Console.WriteLine(message);
#endif
        }

        /// <summary> An ordinary line, formatted only if it is going to be written. </summary>
        public static void MeowFormat(string format, params object[] args)
        {
#if BHSDK_UNITY
            Debug.LogFormat(format, args);
#else
            Console.WriteLine(format, args);
#endif
        }

        /// <summary> Something worth noticing that changed nothing. </summary>
        public static void MeowWarn(object message)
        {
#if BHSDK_UNITY
            Debug.LogWarning(message);
#else
            Console.WriteLine($"[WARN] {message}");
#endif
        }

        /// <summary> The same, formatted. </summary>
        public static void MeowWarnFormat(string format, params object[] args)
        {
#if BHSDK_UNITY
            Debug.LogWarningFormat(format, args);
#else
            Console.WriteLine($"[WARN] {string.Format(format, args)}");
#endif
        }

        /// <summary> Something that went wrong. </summary>
        public static void MeowError(object message)
        {
#if BHSDK_UNITY
            Debug.LogError(message);
#else
            Console.WriteLine($"[ERROR] {message}");
#endif
        }

        /// <summary> The same, formatted. </summary>
        public static void MeowErrorFormat(string format, params object[] args)
        {
#if BHSDK_UNITY
            Debug.LogErrorFormat(format, args);
#else
            Console.WriteLine($"[ERROR] {string.Format(format, args)}");
#endif
        }

        /// <summary> An exception with its stack, where the engine's console can expand it. </summary>
        public static void MeowException(Exception exception)
        {
#if BHSDK_UNITY
            Debug.LogException(exception);
#else
            Console.WriteLine($"[EXCEPTION] {exception}");
#endif
        }
    }
}