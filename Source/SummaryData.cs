using System.Collections.Generic;
using RimWorld;
using UIRethemed.Core;
using Verse;

namespace ColonistSummary
{
    // Listed rows, the page, search, sorting and title bar counts; rows are rebuilt once a real second while the game runs and when the
    // page, search, sort or settings change. Lifetime: the window (Clear on open and close); the page and sort choice last for the session.
    public static class SummaryData
    {
        private const float RefreshSeconds = 1f;

        public static readonly List<PawnSummary> all = new List<PawnSummary>();
        public static readonly List<PawnSummary> shown = new List<PawnSummary>();
        public static readonly List<SummaryColumn> columns = new List<SummaryColumn>();
        public static readonly List<SummaryStat> stats = new List<SummaryStat>();

        public static SummaryPage page = SummaryPage.Summary;
        public static string search = "";
        public static SummaryColumn sortBy = SummaryColumns.Usual;
        public static bool descending;

        public static int colonists;
        public static float columnsWidth;
        // Raised on every rebuild, so the table can re-lay out
        public static int version;
        public static string countText = "";

        private static readonly List<Pawn> tmpPawns = new List<Pawn>();
        private static readonly List<Pawn> tmpOthers = new List<Pawn>();
        private static readonly System.Comparison<PawnSummary> comparison = CompareRows;
        private static int builtTick = int.MinValue;
        private static float builtAt;
        private static bool dirty = true;

        public static void SetDirty() => dirty = true;

        public static void Clear()
        {
            all.Clear();
            shown.Clear();
            stats.Clear();
            search = "";
            dirty = true;
            builtTick = int.MinValue;
            builtAt = 0f;
            SummaryCache.Clear();
        }

        // Rebuilds when asked to, otherwise at most once a real second while the game runs, whatever its speed
        public static void Refresh()
        {
            int now = Find.TickManager.TicksGame;
            float realNow = ThemeMotion.Now;
            if (!dirty && (now == builtTick || realNow - builtAt < RefreshSeconds)) return;
            dirty = false;
            builtTick = now;
            builtAt = realNow;
            Rebuild();
        }

        public static void SetPage(SummaryPage value)
        {
            if (page == value) return;
            page = value;
            sortBy = SummaryColumns.Usual;
            descending = false;
            dirty = true;
        }

        public static void SortOn(SummaryColumn column)
        {
            // Each heading cycles: its starting direction, reversed, then back to the colonist bar's order
            if (sortBy == column && column != SummaryColumns.Usual && descending != column.startsDescending)
            {
                sortBy = SummaryColumns.Usual;
                descending = false;
            }
            else if (sortBy == column) descending = !descending;
            else
            {
                sortBy = column;
                descending = column.startsDescending;
            }
            dirty = true;
        }

        private static void Rebuild()
        {
            bool health = page == SummaryPage.Health;
            ColonistSummarySettings settings = ColonistSummaryMod.Settings;

            // Same colonists as the vanilla Assign tab: free colonists on every map, in caravans and in transporters, except babies
            tmpPawns.Clear();
            tmpPawns.AddRange(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists);
            tmpPawns.RemoveAll(p => p.DevelopmentalStage.Baby());
            PlayerPawnsDisplayOrderUtility.Sort(tmpPawns);
            colonists = tmpPawns.Count;

            all.Clear();
            AddRows(tmpPawns, RowKind.Colonist, health);
            if (health && settings.healthPrisoners)
            {
                tmpOthers.Clear();
                tmpOthers.AddRange(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_PrisonersOfColony);
                tmpOthers.SortBy(p => p.LabelShortCap);
                AddRows(tmpOthers, RowKind.Prisoner, true);
            }
            if (health && settings.healthAnimals)
            {
                tmpOthers.Clear();
                tmpOthers.AddRange(PawnsFinder.AllMaps_SpawnedPawnsInFaction(Faction.OfPlayer));
                tmpOthers.RemoveAll(p => !p.RaceProps.Animal || p.Dead);
                tmpOthers.SortBy(p => p.def.label, p => p.LabelShortCap);
                AddRows(tmpOthers, RowKind.Animal, true);
            }
            tmpPawns.Clear();
            tmpOthers.Clear();

            columns.Clear();
            columnsWidth = 0f;
            List<SummaryColumn> every = SummaryColumns.For(page);
            for (int i = 0; i < every.Count; i++)
            {
                if (!every[i].Shown) continue;
                every[i].Measure();
                columns.Add(every[i]);
                columnsWidth += every[i].width;
            }
            BlockHeader.Measure(columns);
            if (!columns.Contains(sortBy) && sortBy != SummaryColumns.Usual && sortBy != SummaryColumns.Name)
            {
                sortBy = SummaryColumns.Usual;
                descending = false;
            }

            shown.Clear();
            string needle = search.Trim().ToLowerInvariant();
            for (int i = 0; i < all.Count; i++)
            {
                if (needle.Length == 0 || all[i].searchText.Contains(needle)) shown.Add(all[i]);
            }
            shown.Sort(comparison);

            if (shown.Count != all.Count) countText = "ColonistSummary_ShownOf".Translate(shown.Count, all.Count).ToString();
            else if (all.Count == colonists) countText = "ColonistSummary_ColonistCount".Translate(all.Count).ToString();
            else countText = "ColonistSummary_ListedCount".Translate(all.Count).ToString();

            stats.Clear();
            if (health) HealthStats();
            else SummaryStats();
            version++;
        }

        private static void AddRows(List<Pawn> pawns, RowKind kind, bool health)
        {
            for (int i = 0; i < pawns.Count; i++)
            {
                PawnSummary row = SummaryCache.Get(pawns[i], health);
                row.kind = kind;
                row.order = all.Count;
                all.Add(row);
            }
        }

        private static void SummaryStats()
        {
            int bleeding = 0;
            int uncomfortable = 0;
            int mental = 0;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].bleedText.Length > 0) bleeding++;
                if (!all[i].comfyNow) uncomfortable++;
                if (all[i].mentalState.Length > 0) mental++;
            }
            stats.Add(new SummaryStat(SummaryColumns.ColonistsTex, all.Count, Theme.HighlightGreen, "ColonistSummary_Stat_Colonists".Translate(), "ColonistSummary_Stat_ColonistsTip".Translate()));
            stats.Add(new SummaryStat(SummaryColumns.BleedingTex, bleeding, Theme.HighlightRed, "ColonistSummary_Stat_Bleeding".Translate(), "ColonistSummary_Stat_BleedingTip".Translate()));
            stats.Add(new SummaryStat(SummaryColumns.MentalTex, mental, Theme.HighlightRed, "ColonistSummary_Stat_Mental".Translate(), "ColonistSummary_Stat_MentalTip".Translate()));
            stats.Add(new SummaryStat(SummaryColumns.TemperatureTex, uncomfortable, Theme.HighlightYellow, "ColonistSummary_Stat_Uncomfortable".Translate(), "ColonistSummary_Stat_UncomfortableTip".Translate()));
        }

        private static void HealthStats()
        {
            int downed = 0;
            int tending = 0;
            int surgery = 0;
            int bleeding = 0;
            for (int i = 0; i < all.Count; i++)
            {
                HealthSummary health = all[i].Health;
                if (health.downed) downed++;
                if (health.untended > 0) tending++;
                if (health.operations.count > 0) surgery++;
                if (all[i].bleedText.Length > 0) bleeding++;
            }
            int medicine = 0;
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].IsPlayerHome) medicine += maps[i].resourceCounter.GetCountIn(ThingCategoryDefOf.Medicine);
            }
            stats.Add(new SummaryStat(SummaryColumns.DownedTex, downed, Theme.HighlightRed, "ColonistSummary_Stat_Downed".Translate(), "ColonistSummary_Stat_DownedTip".Translate()));
            stats.Add(new SummaryStat(SummaryColumns.TendingTex, tending, Theme.HighlightYellow, "ColonistSummary_Stat_Tending".Translate(), "ColonistSummary_Stat_TendingTip".Translate()));
            stats.Add(new SummaryStat(SummaryColumns.OperationsTex, surgery, Theme.HighlightGreen, "ColonistSummary_Stat_Surgery".Translate(), "ColonistSummary_Stat_SurgeryTip".Translate()));
            stats.Add(new SummaryStat(SummaryColumns.BleedingTex, bleeding, Theme.HighlightRed, "ColonistSummary_Stat_Bleeding".Translate(), "ColonistSummary_Stat_BleedingTip".Translate()));
            stats.Add(new SummaryStat(SummaryColumns.MedicineTex, medicine, Theme.HighlightGreen, "ColonistSummary_Stat_Medicine".Translate(), "ColonistSummary_Stat_MedicineTip".Translate()));
        }

        // Ties keep the usual order
        private static int CompareRows(PawnSummary a, PawnSummary b)
        {
            int result = sortBy.Compare(a, b);
            if (descending) result = -result;
            return result != 0 ? result : a.order.CompareTo(b.order);
        }
    }
}
