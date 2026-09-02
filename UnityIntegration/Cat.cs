using System;
#if BHSDK_UNITY
using UnityEngine;
#endif

namespace BH.SDK
{
    public static class Cat
    {
        public static void Meow(object message)
        {
#if BHSDK_UNITY
            Debug.Log(message);
#else
            Console.WriteLine(message);
#endif
        }
        public static void MeowFormat(string format, params object[] args)
        {
#if BHSDK_UNITY
            Debug.LogFormat(format, args);
#else
            Console.WriteLine(format, args);
#endif
        }
        public static void MeowWarn(object message)
        {
#if BHSDK_UNITY
            Debug.LogWarning(message);
#else
            Console.WriteLine($"[WARN] {message}");
#endif
        }
        public static void MeowWarnFormat(string format, params object[] args)
        {
#if BHSDK_UNITY
            Debug.LogWarningFormat(format, args);
#else
            Console.WriteLine($"[WARN] {string.Format(format, args)}");
#endif
        }
        public static void MeowError(object message)
        {
#if BHSDK_UNITY
            Debug.LogError(message);
#else
            Console.WriteLine($"[ERROR] {message}");
#endif
        }
        public static void MeowErrorFormat(string format, params object[] args)
        {
#if BHSDK_UNITY
            Debug.LogErrorFormat(format, args);
#else
            Console.WriteLine($"[ERROR] {string.Format(format, args)}");
#endif
        }
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