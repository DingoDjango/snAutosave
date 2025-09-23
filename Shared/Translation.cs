using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using LitJson;

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
        internal static string Translate(this string source)
        {
            if (Language.main.TryGet(source, out string translated))
            {
                return translated;
            }

            ModPlugin.LogMessage($"Could not find translated string for `{source}`");

            return source;
        }

        internal static string FormatTranslate(this string source, string arg0)
        {
            string basic = source.Translate();

            if (!string.IsNullOrEmpty(arg0))
            {
                try
                {
                    return string.Format(basic, arg0);
                }

                catch (Exception ex)
                {
                    ModPlugin.LogMessage(ex.ToString());
                    ModPlugin.LogMessage($"Failed to format '{source}' with arg0 `{arg0}'");
                }
            }

            return basic;
        }

        internal static string GetCustomDateFormat(DateTime dateTime)
        {
            CultureInfo culture = (CultureInfo)typeof(Language).GetField("currentCultureInfo", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(Language.main);

#if DEBUG
            ModPlugin.LogMessage($"culture == {culture}.");
#endif

            string customFormat = DateTimeFormatLibrary.DateTimes[ModPlugin.ConfigCustomDateTimeFormat.Value];
            object[] args = new object[] { dateTime };
            string formattedDate = string.Format(culture, customFormat, args);

#if DEBUG
            ModPlugin.LogMessage($"GetCustomDateFormat == {formattedDate}.");
#endif

            return formattedDate;
        }
    }
}
