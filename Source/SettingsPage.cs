using System;
using System.Collections.Generic;
using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // The mod settings page on Core's settings cards: the Window card with the tab's size, the Columns card with each page's columns
    // and who the Health page lists
    [StaticConstructorOnStartup]
    public static class SettingsPage
    {
        private const string WindowKey = "Window";
        private const string ColumnsKey = "Columns";

        private static readonly Texture2D WindowTex = CoreWidgets.Icon("frame-corners");
        private static readonly Texture2D ColumnsTex = CoreWidgets.Icon("eye");
        private static readonly Texture2D FitTex = CoreWidgets.Icon("users");
        private static readonly Texture2D PrisonerTex = CoreWidgets.Icon("lock");
        private static readonly Texture2D AnimalTex = CoreWidgets.Icon("paw-print");
        // Every card starts open; a player's collapse lasts for the session
        private static readonly HashSet<string> open = new HashSet<string> { WindowKey, ColumnsKey };
        private static readonly SettingsCards cards = new SettingsCards("ColonistSummary", () => open);
        private static readonly Func<float, float, float> allCards = AllCards;
        private static readonly Action<Listing_Standard> windowCard = WindowCard;
        private static readonly Action<Listing_Standard> columnsCard = ColumnsCard;

        // Session lifetime, from translations only
        private static readonly string tabName = "ColonistSummary_Settings_TabName".Translate();
        private static readonly string windowTitle = "ColonistSummary_Settings_Window".Translate();
        private static readonly string fitLabel = "ColonistSummary_Settings_FitToColonists".Translate();
        private static readonly string fitTip = "ColonistSummary_Settings_FitToColonistsTip".Translate();
        private static readonly string resetLabel = "ColonistSummary_Settings_Reset".Translate();
        private static readonly string resetTip = "ColonistSummary_Settings_ResetTip".Translate();
        private static readonly string resetNothing = "ColonistSummary_Settings_ResetNothing".Translate();
        private static readonly string columnsTitle = "ColonistSummary_Settings_Columns".Translate();
        private static readonly string summaryHeading = "ColonistSummary_Settings_SummaryPage".Translate();
        private static readonly string healthHeading = "ColonistSummary_Settings_HealthPage".Translate();
        private static readonly string prisonersLabel = "ColonistSummary_Settings_Prisoners".Translate();
        private static readonly string prisonersTip = "ColonistSummary_Settings_PrisonersTip".Translate();
        private static readonly string animalsLabel = "ColonistSummary_Settings_Animals".Translate();
        private static readonly string animalsTip = "ColonistSummary_Settings_AnimalsTip".Translate();
        private static readonly string columnsResetTip = "ColonistSummary_Settings_ColumnsResetTip".Translate();
        private static readonly string allShown = "ColonistSummary_Settings_AllShown".Translate();
        // Tips for columns whose DLC is missing, built on first draw
        private static readonly Dictionary<SummaryColumn, string> needsTips = new Dictionary<SummaryColumn, string>();

        private static Vector2 scroll;
        private static float contentHeight;
        // Card summaries, rebuilt when the settings they describe change
        private static string summary = "";
        private static bool lastFitWidth;
        private static float lastWidth;
        private static bool lastFitHeight;
        private static float lastHeight;
        private static bool lastFit;
        private static string columnsSummary = "";
        private static int lastHiddenCount = -1;
        private static bool lastPrisoners;
        private static bool lastAnimals;

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
            y = cards.Card(y, width, WindowKey, WindowTex, windowTitle, Summary(), windowCard);
            return cards.Card(y, width, ColumnsKey, ColumnsTex, columnsTitle, ColumnsSummary(), columnsCard);
        }

        private static void WindowCard(Listing_Standard listing)
        {
            ColonistSummarySettings settings = Settings;
            settings.size.DoSettings(listing, tabName, cards);
            SettingsCards.Tick(listing, FitTex, Theme.TextDim, fitLabel, ref settings.fitToColonists, fitTip, true);
            bool changed = !settings.size.IsDefault || !settings.fitToColonists;
            if (SettingsCards.ResetButton(listing, resetLabel, changed, resetTip, resetNothing))
            {
                settings.ResetWindow();
            }
        }

        private static void ColumnsCard(Listing_Standard listing)
        {
            ColonistSummarySettings settings = Settings;
            SettingsCards.Subheading(listing, summaryHeading);
            Ticks(listing, SummaryColumns.SummaryChoices);
            SettingsCards.Subheading(listing, healthHeading);
            if (SettingsCards.Tick(listing, PrisonerTex, Theme.TextDim, prisonersLabel, ref settings.healthPrisoners, prisonersTip, true)) SummaryData.SetDirty();
            if (SettingsCards.Tick(listing, AnimalTex, Theme.TextDim, animalsLabel, ref settings.healthAnimals, animalsTip, true)) SummaryData.SetDirty();
            Ticks(listing, SummaryColumns.HealthChoices);
            bool changed = settings.AnyHidden || !settings.healthPrisoners || settings.healthAnimals;
            if (SettingsCards.ResetButton(listing, resetLabel, changed, columnsResetTip, resetNothing))
            {
                settings.ResetColumns();
            }
        }

        // One tick per column or block; a column whose DLC is missing is greyed out with a tip naming it
        private static void Ticks(Listing_Standard listing, List<SummaryColumn> choices)
        {
            ColonistSummarySettings settings = Settings;
            for (int i = 0; i < choices.Count; i++)
            {
                SummaryColumn column = choices[i];
                string? needs = column.Requirement;
                bool on = !settings.IsHidden(column.hideKey);
                string tip = needs == null ? column.tip : NeedsTip(column, needs);
                Texture2D? icon = column.icon as Texture2D;
                if (SettingsCards.Tick(listing, icon!, Theme.TextDim, column.label, ref on, tip, needs == null))
                {
                    settings.SetHidden(column.hideKey, !on);
                }
            }
        }

        private static string NeedsTip(SummaryColumn column, string dlc)
        {
            if (!needsTips.TryGetValue(column, out string? tip))
            {
                tip = column.tip + "\n\n" + "ColonistSummary_Settings_NeedsDlc".Translate(dlc);
                needsTips[column] = tip;
            }
            return tip;
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

        private static string ColumnsSummary()
        {
            ColonistSummarySettings settings = Settings;
            int hidden = HiddenCount(SummaryColumns.SummaryChoices) + HiddenCount(SummaryColumns.HealthChoices);
            if (hidden != lastHiddenCount || settings.healthPrisoners != lastPrisoners || settings.healthAnimals != lastAnimals || columnsSummary.Length == 0)
            {
                lastHiddenCount = hidden;
                lastPrisoners = settings.healthPrisoners;
                lastAnimals = settings.healthAnimals;
                string columns = hidden == 0 ? allShown : "ColonistSummary_Settings_HiddenCount".Translate(hidden).ToString();
                string who = settings.healthPrisoners && settings.healthAnimals ? "ColonistSummary_Settings_ListsBoth".Translate().ToString()
                    : settings.healthPrisoners ? "ColonistSummary_Settings_ListsPrisoners".Translate().ToString()
                    : settings.healthAnimals ? "ColonistSummary_Settings_ListsAnimals".Translate().ToString()
                    : "ColonistSummary_Settings_ListsColonists".Translate().ToString();
                columnsSummary = columns + ", " + who;
            }
            return columnsSummary;
        }

        private static int HiddenCount(List<SummaryColumn> choices)
        {
            int count = 0;
            for (int i = 0; i < choices.Count; i++)
            {
                if (Settings.IsHidden(choices[i].hideKey)) count++;
            }
            return count;
        }
    }
}
