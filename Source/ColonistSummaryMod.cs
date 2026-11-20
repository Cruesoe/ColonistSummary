using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // Loads the settings and draws them on Core's settings cards
    public class ColonistSummaryMod : Mod
    {
        public static ColonistSummaryMod? Instance { get; private set; }
        public static ColonistSummarySettings Settings { get; private set; } = null!;

        public ColonistSummaryMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<ColonistSummarySettings>();
        }

        public override string SettingsCategory() => Content.Name;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            SettingsPage.Draw(inRect.LeftPart(0.5f));
            SettingsCards.ThemeButton(inRect, SettingsCategory());
            Theme.Reset();
        }
    }
}
