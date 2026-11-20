using System;
using System.Reflection;
using Verse;

namespace ColonySummary
{
    // Reads the header style set in UI Rethemed - Work's settings, or Classic without it; members are resolved once per session
    public static class WorkHeaderStyle
    {
        private const string WorkPackageId = "LazyFridayStudio.UIRethemed.Work";

        private static bool resolved;
        private static FieldInfo? settingsField;
        private static FieldInfo? styleField;

        public static SkillHeaderStyle Read()
        {
            if (!resolved)
            {
                resolved = true;
                Resolve();
            }
            if (settingsField == null || styleField == null) return SkillHeaderStyle.Classic;
            try
            {
                object? settings = settingsField.GetValue(null);
                if (settings == null) return SkillHeaderStyle.Classic;
                int value = Convert.ToInt32(styleField.GetValue(settings));
                return Enum.IsDefined(typeof(SkillHeaderStyle), value) ? (SkillHeaderStyle)value : SkillHeaderStyle.Classic;
            }
            catch (Exception)
            {
                settingsField = null;
                return SkillHeaderStyle.Classic;
            }
        }

        private static void Resolve()
        {
            if (!ModsConfig.IsActive(WorkPackageId)) return;
            Type? mod = GenTypes.GetTypeInAnyAssembly("UIRethemed.Work.UIRethemedWorkMod");
            settingsField = mod?.GetField("Settings", BindingFlags.Public | BindingFlags.Static);
            styleField = settingsField?.FieldType.GetField("headerStyle", BindingFlags.Public | BindingFlags.Instance);
            if (mod != null && (settingsField == null || styleField == null))
            {
                Log.Warning("[Colony Summary] Couldn't read UI Rethemed - Work's header style; skill headers use the classic style.");
            }
        }
    }
}
