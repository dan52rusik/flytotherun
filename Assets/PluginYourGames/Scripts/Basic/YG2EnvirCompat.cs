using System;
using UnityEngine;

namespace YG
{
    public static partial class YG2
    {
        private static readonly EnvirCompat _envirCompat = new EnvirCompat();
        public static EnvirCompat envir => _envirCompat;

        public sealed class EnvirCompat
        {
            public string domain => ResolveDomain();
            public string language => YG2.lang;
            public bool isMobile => ResolveIsMobile();
            public bool isTablet => ResolveIsTablet();

            private static string ResolveDomain()
            {
                if (!string.IsNullOrEmpty(Application.absoluteURL))
                {
                    try
                    {
                        Uri uri = new Uri(Application.absoluteURL);
                        string host = uri.Host.ToLowerInvariant();

                        if (host.Contains("yandex.ru"))
                            return "ru";

                        if (host.Contains("yandex.com"))
                            return "com";

                        int domainIndex = host.IndexOf("yandex.", StringComparison.Ordinal);
                        if (domainIndex >= 0)
                        {
                            string suffix = host.Substring(domainIndex + "yandex.".Length);
                            if (!string.IsNullOrEmpty(suffix))
                                return suffix;
                        }
                    }
                    catch
                    {
                    }
                }

                string currentLang = YG2.lang;
                if (string.IsNullOrEmpty(currentLang))
                    currentLang = Application.systemLanguage.ToString().ToLowerInvariant();

                return currentLang.StartsWith("ru") ? "ru" : "com";
            }

            private static bool ResolveIsMobile()
            {
#if UNITY_EDITOR
                return YG2.infoYG.Simulation.device == Device.Mobile;
#else
                return Application.isMobilePlatform && !ResolveIsTablet();
#endif
            }

            private static bool ResolveIsTablet()
            {
#if UNITY_EDITOR
                return YG2.infoYG.Simulation.device == Device.Tablet;
#else
                if (!Application.isMobilePlatform)
                    return false;

                float dpi = Screen.dpi;
                if (dpi <= 0f)
                    return false;

                float widthInches = Mathf.Min(Screen.width, Screen.height) / dpi;
                float heightInches = Mathf.Max(Screen.width, Screen.height) / dpi;
                float diagonal = Mathf.Sqrt(widthInches * widthInches + heightInches * heightInches);

                return diagonal >= 6.5f;
#endif
            }
        }
    }
}
