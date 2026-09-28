using System.Diagnostics;

namespace MZ.Ads
{
    /// <summary>
    /// Plugin logging. <see cref="Info"/> calls are compiled out unless ADS_VERBOSE_LOG is defined,
    /// so release builds carry no log spam. Warnings and errors are always kept.
    /// </summary>
    public static class AdsLog
    {
        private const string Tag = "[MZ.Ads] ";

        [Conditional("ADS_VERBOSE_LOG")]
        public static void Info(string message)
        {
            UnityEngine.Debug.Log(Tag + message);
        }

        public static void Warning(string message)
        {
            UnityEngine.Debug.LogWarning(Tag + message);
        }

        public static void Error(string message)
        {
            UnityEngine.Debug.LogError(Tag + message);
        }
    }
}
