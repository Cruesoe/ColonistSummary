using System;
using System.Collections.Generic;
using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // The mod settings page on Core's settings cards: the Window card on the left, and on the right a card per page with its columns
    // (and who the Health page lists), so the long lists get the full height
    [StaticConstructorOnStartup]
    public static class SettingsPage
    {
        private const string WindowKey = "Window";
        private const string SummaryKey = "SummaryColumns";
        private const string HealthKey = "HealthColumns";
        private const float Gap = 12f;

        private static readonly Texture2D WindowTex = CoreWidgets.Icon("frame-corners");
        private static readonly Texture2D FitTex = CoreWidgets.Icon("users");
        private static readonly Texture2D PrisonerTex = CoreWidgets.Icon("lock");
        private static readonly Texture2D AnimalTex = CoreWidgets.Icon("paw-print");
        // Every card starts open; a player's collapse lasts for the session
        private static readonly HashSet<string> open = new HashSet<string> { WindowKey, SummaryKey, HealthKey };
        private static readonly SettingsCards cards = new SettingsCards("ColonistSummary", () => open);
        private static readonly Func<float, float, float> leftCards = LeftCards;
        private static readonly Func<float, float, float> rightCards = RightCards;
        private static readonly Action<Listing_Standard> windowCard = WindowCard;
        private static readonly Action<Listing_Standard> summaryCard = SummaryCard;
        private static readonly Action<Listing_Standard> healthCard = HealthCard;

        // Session lifetime, from translations only
        private static readonly string tabName = "ColonistSummary_Settings_TabName".Translate();
        private static readonly string windowTitle = "ColonistSummary_Settings_Window".Translate();
        private static readonly string fitLabel = "ColonistSummary_Settings_FitToColonists".Translate();
        private static readonly string fitTip = "ColonistSummary_Settings_FitToColonistsTip".Translate();
        private static readonly string resetLabel = "ColonistSummary_Settings_Reset".Translate();
        private static readonly string resetTip = "ColonistSummary_Settings_ResetTip".Translate();
        private static readonly string resetNothing = "ColonistSummary_Settings_ResetNothing".Translate();
        private static readonly string summaryTitle = "ColonistSummary_Settings_SummaryPage".Translate();
        private static readonly string healthTitle = "ColonistSummary_Settings_HealthPage".Translate();
        private static readonly string prisonersLabel = "ColonistSummary_Settings_Prisoners".Translate();
        private static readonly string prisonersTip = "ColonistSummary_Settings_PrisonersTip".Translate();
        private static readonly string animalsLabel = "ColonistSummary_Settings_Animals".Translate();
        private static readonly string animalsTip = "ColonistSummary_Settings_AnimalsTip".Translate();
        private static readonly string columnsResetLabel = "ColonistSummary_Settings_ColumnsReset".Translate();
        private static readonly string columnsResetTip = "ColonistSummary_Settings_ColumnsResetTip".Translate();
        private static readonly string columnsResetNothing = "ColonistSummary_Settings_ColumnsResetNothing".Translate();
        private static readonly string allShown = "ColonistSummary_Settings_AllShown".Translate();
        // Tips for columns whose DLC is missing, built on first draw
        private static readonly Dictionary<SummaryColumn, string> needsTips = new Dictionary<SummaryColumn, string>();

        private static Vector2 leftScroll;
        private static float leftHeight;
        private static Vector2 rightScroll;
        private static float rightHeight;
        // Card summaries, rebuilt when the settings they describe change
        private static string summary = "";
        private static bool lastFitWidth;
        private static float lastWidth;
        private static bool lastFitHeight;
        private static float lastHeight;
        private static bool lastFit;
        private static string summaryColumnsSummary = "";
        private static string healthColumnsSummary = "";
        private static int lastSummaryHidden = -1;
        private static int lastHealthHidden = -1;
        private static bool lastPrisoners;
        private static bool lastAnimals;

        private static ColonistSummarySettings Settings => ColonistSummaryMod.Settings;

        public static void Draw(Rect rect)
        {
            float half = Mathf.Floor((rect.width - Gap) / 2f);
            try
            {
                leftHeight = cards.Column(new Rect(rect.x, rect.y, half, rect.height), ref leftScroll, leftHeight, leftCards);
                rightHeight = cards.Column(new Rect(rect.xMax - half, rect.y, half, rect.height), ref rightScroll, rightHeight, rightCards);
            }
            finally
            {
                Theme.CloseScrolls();
                Theme.Reset();
            }
            cards.Finish();
        }

        private static float LeftCards(float y, float width) => cards.Card(y, width, WindowKey, WindowTex, windowTitle, Summary(), windowCard);

        private static float RightCards(float y, float width)
        {
            RefreshColumnSummaries();
            y = cards.Card(y, width, SummaryKey, SummaryColumns.ColonistsTex, summaryTitle, summaryColumnsSummary, summaryCard);
            return cards.Card(y + 8f, width, HealthKey, SummaryColumns.HealthTex, healthTitle, healthColumnsSummary, healthCard);
        }

        private static void WindowCard(Listing_Standard listing)
        {
            Inset(listing);
            ColonistSummarySettings settings = Settings;
            settings.size.DoSettings(listing, tabName, cards);
            SettingsCards.Tick(listing, FitTex, Theme.TextDim, fitLabel, ref settings.fitToColonists, fitTip, true);
            bool changed = !settings.size.IsDefault || !settings.fitToColonists;
            if (SettingsCards.ResetButton(listing, resetLabel, changed, resetTip, resetNothing))
            {
                settings.ResetWindow();
            }
        }

        private static void SummaryCard(Listing_Standard listing)
        {
            Inset(listing);
            Ticks(listing, SummaryColumns.SummaryChoices);
        }

        // Keeps the rows clear of the card body's right edge, which can clip a tick box's border by a pixel
        private static void Inset(Listing_Standard listing) => listing.ColumnWidth -= 2f;

        private static void HealthCard(Listing_Standard listing)
        {
            Inset(listing);
            ColonistSummarySettings settings = Settings;
            if (SettingsCards.Tick(listing, PrisonerTex, Theme.TextDim, prisonersLabel, ref settings.healthPrisoners, prisonersTip, true)) SummaryData.SetDirty();
            if (SettingsCards.Tick(listing, AnimalTex, Theme.TextDim, animalsLabel, ref settings.healthAnimals, animalsTip, true)) SummaryData.SetDirty();
            Ticks(listing, SummaryColumns.HealthChoices);
            bool changed = settings.AnyHidden || !settings.healthPrisoners || settings.healthAnimals;
            if (SettingsCards.ResetButton(listing, columnsResetLabel, changed, columnsResetTip, columnsResetNothing))
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

        // Summaries for the two column cards, rebuilt when what they count changes
        private static void RefreshColumnSummaries()
        {
            ColonistSummarySettings settings = Settings;
            int summaryHidden = HiddenCount(SummaryColumns.SummaryChoices);
            if (summaryHidden != lastSummaryHidden || summaryColumnsSummary.Length == 0)
            {
                lastSummaryHidden = summaryHidden;
                summaryColumnsSummary = Hidden(summaryHidden);
            }
            int healthHidden = HiddenCount(SummaryColumns.HealthChoices);
            if (healthHidden != lastHealthHidden || settings.healthPrisoners != lastPrisoners || settings.healthAnimals != lastAnimals || healthColumnsSummary.Length == 0)
            {
                lastHealthHidden = healthHidden;
                lastPrisoners = settings.healthPrisoners;
                lastAnimals = settings.healthAnimals;
                string who = settings.healthPrisoners && settings.healthAnimals ? "ColonistSummary_Settings_ListsBoth".Translate().ToString()
                    : settings.healthPrisoners ? "ColonistSummary_Settings_ListsPrisoners".Translate().ToString()
                    : settings.healthAnimals ? "ColonistSummary_Settings_ListsAnimals".Translate().ToString()
                    : "ColonistSummary_Settings_ListsColonists".Translate().ToString();
                healthColumnsSummary = Hidden(healthHidden) + ", " + who;
            }
        }

        private static string Hidden(int count) => count == 0 ? allShown : "ColonistSummary_Settings_HiddenCount".Translate(count).ToString();

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
