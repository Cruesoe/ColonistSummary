using System;
using System.Collections.Generic;
using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // The mod settings page on Core's settings cards: the Window card with the tab's size
    [StaticConstructorOnStartup]
    public static class SettingsPage
    {
        private const string WindowKey = "Window";

        private static readonly Texture2D WindowTex = CoreWidgets.Icon("frame-corners");
        private static readonly Texture2D FitTex = CoreWidgets.Icon("users");
        private static readonly Texture2D ResetTex = CoreWidgets.Icon("arrow-counter-clockwise");
        // Every card starts open; a player's collapse lasts for the session
        private static readonly HashSet<string> open = new HashSet<string> { WindowKey };
        private static readonly SettingsCards cards = new SettingsCards("ColonistSummary", () => open);
        private static readonly Func<float, float, float> allCards = AllCards;
        private static readonly Action<Listing_Standard> windowCard = WindowCard;

        // Session lifetime, from translations only
        private static readonly string tabName = "ColonistSummary_Settings_TabName".Translate();
        private static readonly string windowTitle = "ColonistSummary_Settings_Window".Translate();
        private static readonly string fitLabel = "ColonistSummary_Settings_FitToColonists".Translate();
        private static readonly string fitTip = "ColonistSummary_Settings_FitToColonistsTip".Translate();
        private static readonly string resetLabel = "ColonistSummary_Settings_Reset".Translate();
        private static readonly string resetTip = "ColonistSummary_Settings_ResetTip".Translate();
        private static readonly string resetNothing = "ColonistSummary_Settings_ResetNothing".Translate();

        private static Vector2 scroll;
        private static float contentHeight;
        // Window card summary, rebuilt when the settings it describes change
        private static string summary = "";
        private static bool lastFitWidth;
        private static float lastWidth;
        private static bool lastFitHeight;
        private static float lastHeight;
        private static bool lastFit;

        private static ColonistSummarySettings Settings => ColonistSummaryMod.Settings;

        public static void Draw(Rect rect)
        {
            try
            {
                contentHeight = cards.Column(rect, ref scroll, contentHeight, allCards);
            }
            finally
            {
                Theme.CloseScrolls();
                Theme.Reset();
            }
            cards.Finish();
        }

        private static float AllCards(float y, float width)
        {
            return cards.Card(y, width, WindowKey, WindowTex, windowTitle, Summary(), windowCard);
        }

        private static void WindowCard(Listing_Standard listing)
        {
            ColonistSummarySettings settings = Settings;
            settings.size.DoSettings(listing, tabName, cards);
            SettingsCards.Tick(listing, FitTex, Theme.TextDim, fitLabel, ref settings.fitToColonists, fitTip, true);
            bool changed = !settings.size.IsDefault || !settings.fitToColonists;
            if (SettingsCards.ResetButton(listing, resetLabel, changed, resetTip, resetNothing))
            {
                settings.Reset();
            }
        }

        private static string Summary()
        {
            TabSize size = Settings.size;
            bool fit = Settings.fitToColonists;
            if (summary.Length == 0 || size.fitWidth != lastFitWidth || size.widthScale != lastWidth || size.fitHeight != lastFitHeight || size.heightScale != lastHeight || fit != lastFit)
            {
                lastFitWidth = size.fitWidth;
                lastWidth = size.widthScale;
                lastFitHeight = size.fitHeight;
                lastHeight = size.heightScale;
                lastFit = fit;
                summary = fit ? "ColonistSummary_Settings_WindowSummaryFit".Translate(size.Summary).ToString() : size.Summary;
            }
            return summary;
        }
    }
}
