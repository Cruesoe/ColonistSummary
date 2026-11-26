using System;
using System.Reflection;
using Verse;

namespace ColonistSummary
{
    // Reads the header style and column outlines set in UI Rethemed - Work's settings, or Classic without it; members are resolved once per session
    public static class WorkHeaderStyle
    {
        private const string WorkPackageId = "LazyFridayStudio.UIRethemed.Work";

        private static bool resolved;
        private static FieldInfo? settingsField;
        private static FieldInfo? styleField;
        private static FieldInfo? outlinesField;

        public static SkillHeaderStyle Read()
        {
            object? settings = Settings();
            if (settings == null || styleField == null) return SkillHeaderStyle.Classic;
            try
            {
                int value = Convert.ToInt32(styleField.GetValue(settings));
                return Enum.IsDefined(typeof(SkillHeaderStyle), value) ? (SkillHeaderStyle)value : SkillHeaderStyle.Classic;
            }
            catch (Exception)
            {
                settingsField = null;
                return SkillHeaderStyle.Classic;
            }
        }

        // Work's "Column outlines" setting, on by default as in Work
        public static bool Outlines()
        {
            object? settings = Settings();
            if (settings == null || outlinesField == null) return true;
            try
            {
                return outlinesField.GetValue(settings) is bool on ? on : true;
            }
            catch (Exception)
            {
                outlinesField = null;
                return true;
            }
        }

        private static object? Settings()
        {
            if (!resolved)
            {
                resolved = true;
                Resolve();
            }
            try
            {
                return settingsField?.GetValue(null);
            }
            catch (Exception)
            {
                settingsField = null;
                return null;
            }
        }

        private static void Resolve()
        {
            if (!ModsConfig.IsActive(WorkPackageId)) return;
            Type? mod = GenTypes.GetTypeInAnyAssembly("UIRethemed.Work.UIRethemedWorkMod");
            settingsField = mod?.GetField("Settings", BindingFlags.Public | BindingFlags.Static);
            styleField = settingsField?.FieldType.GetField("headerStyle", BindingFlags.Public | BindingFlags.Instance);
            outlinesField = settingsField?.FieldType.GetField("columnOutlines", BindingFlags.Public | BindingFlags.Instance);
            if (mod != null && (settingsField == null || styleField == null))
            {
                Log.Warning("[Colonist Summary] Couldn't read UI Rethemed - Work's header style; skill headers use the classic style.");
            }
        }
    }
}
