using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace SubnauticaAutosave
{
    public enum DateTimeFormat
    {
        [Description("dd M yyyy (24h) → 22 May 2025 - 15:30")]
        DMMMYYYY_24Hour,
        [Description("dd.mm.yyyy (24h) → 22.05.2025 - 15:30")]
        DMY_24Hour,
        [Description("yyyy.mm.dd (24h) → 2025.05.22 - 15:30")]
        YMD_24Hour,
        [Description("dd M yyyy (12h) → 22 May 2025 - 03:30 PM")]
        DMMMYYYY_12Hour,
        [Description("mm.dd.yyyy (12h) → 05.22.2025 - 03:30 PM")]
        MDY_12Hour,
    }

    public static class DateTimeFormatLibrary
    {
        public static readonly Dictionary<DateTimeFormat, string> DateTimes = new Dictionary<DateTimeFormat, string>();

        static DateTimeFormatLibrary()
        {
            DateTimes[DateTimeFormat.DMMMYYYY_24Hour] = "{0:dd MMMM yyyy - HH:mm}";     // 22 May 2025 - 15:30
            DateTimes[DateTimeFormat.DMY_24Hour] = "{0:dd.MM.yyyy - HH:mm}";            // 22.05.2025 - 15:30
            DateTimes[DateTimeFormat.YMD_24Hour] = "{0:yyyy.MM.dd - HH:mm}";            // 2025.05.22 - 15:30
            DateTimes[DateTimeFormat.DMMMYYYY_12Hour] = "{0:dd MMMM yyyy - hh:mm tt}";  // 22 May 2025 - 03:30 PM
            DateTimes[DateTimeFormat.MDY_12Hour] = "{0:MM.dd.yyyy - hh:mm tt}";         // 05.22.2025 - 03:30 PM
        }
    }

    internal static class Translation
    {
    	private static readonly HashSet<string> LoggedMissingKeys = new HashSet<string>();

        private static void LogMissingKey(string source)
        {
            if (LoggedMissingKeys.Add(source))
            {
                ModPlugin.Instance.LogWarning($"Could not find translated string for `{source}`");
            }
        }

        internal static string Translate(this string source)
        {
            if (Language.main.TryGet(source, out string translated))
            {
                return translated;
            }

            LogMissingKey(source);
            return source;
        }

        internal static string FormatTranslate(this string source, params object[] args)
        {
            string basic = source.Translate();

            if (args != null && args.Length > 0)
            {
                try
                {
                    return string.Format(basic, args);
                }
                catch (Exception ex)
                {
                    ModPlugin.Instance.LogError($"Failed to format '{source}': {ex}");
                }
            }

            return basic;
        }

        internal static string TryFormatTranslate(this string source, params object[] args)
        {
            if (!Language.main.TryGet(source, out string basic))
            {
                return null;
            }

            if (args == null || args.Length == 0)
            {
                return basic;
            }

            try
            {
                return string.Format(basic, args);
            }
            catch (Exception ex)
            {
                ModPlugin.Instance.LogMessage($"Failed to format '{source}': {ex}");
                return null;
            }
        }

        internal static string GetCustomDateFormat(DateTime dateTime)
        {
            CultureInfo culture = (CultureInfo)typeof(Language).GetField("currentCultureInfo", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(Language.main);

#if DEBUG
            ModPlugin.Instance.LogMessage($"culture == {culture}.");
#endif

            string customFormat = DateTimeFormatLibrary.DateTimes[ModPlugin.options.CustomDateTimeFormat];
            object[] args = new object[] { dateTime };
            string formattedDate = string.Format(culture, customFormat, args);

#if DEBUG
            ModPlugin.Instance.LogMessage($"GetCustomDateFormat == {formattedDate}.");
#endif

            return formattedDate;
        }
    }
}
