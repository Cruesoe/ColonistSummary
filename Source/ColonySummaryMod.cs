using UnityEngine;
using Verse;

namespace ColonySummary
{
    // Loads the settings and shows them in the mod settings window
    public class ColonySummaryMod : Mod
    {
        // Opened from the Summary tab's settings button
        public static ColonySummaryMod? Instance { get; private set; }

        public ColonySummaryMod(ModContentPack content) : base(content)
        {
            Instance = this;
            GetSettings<ColonySummarySettings>();
        }

        public override string SettingsCategory() => Content.Name;

        public override void DoSettingsWindowContents(Rect inRect) => ColonySummarySettings.DoWindowContents(inRect);
    }
}
