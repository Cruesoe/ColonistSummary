using System.Collections.Generic;
using RimWorld;
using UIRethemed.Core;
using Verse;

namespace ColonistSummary
{
    // Listed colonists, the search, sorting and summary counts; rows are rebuilt once a real second while the game runs and when the search or sort changes.
    // Lifetime: the Summary window (Clear on open and close); the sort choice lasts for the session.
    public static class SummaryData
    {
        private const float RefreshSeconds = 1f;

        public static readonly List<PawnSummary> all = new List<PawnSummary>();
        public static readonly List<PawnSummary> shown = new List<PawnSummary>();
        public static readonly List<SummaryColumn> columns = new List<SummaryColumn>();
        public static readonly List<SummaryStat> stats = new List<SummaryStat>();

        public static string search = "";
        public static SummaryColumn sortBy = SummaryColumns.Usual;
        public static bool descending;

        public static int bleeding;
        public static int uncomfortable;
        public static float columnsWidth;
        // Raised on every rebuild, so the table can re-lay out
        public static int version;
        public static string countText = "";

        private static readonly List<Pawn> tmpPawns = new List<Pawn>();
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

        // Same pawns as the vanilla Assign tab: free colonists on every map, in caravans and in transporters, except babies
        private static void Rebuild()
        {
            tmpPawns.Clear();
            tmpPawns.AddRange(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists);
            tmpPawns.RemoveAll(p => p.DevelopmentalStage.Baby());
            PlayerPawnsDisplayOrderUtility.Sort(tmpPawns);

            all.Clear();
            bleeding = 0;
            uncomfortable = 0;
            for (int i = 0; i < tmpPawns.Count; i++)
            {
                PawnSummary row = SummaryCache.Get(tmpPawns[i]);
                row.order = i;
                all.Add(row);
                if (row.bleedText.Length > 0) bleeding++;
                if (!row.comfyNow) uncomfortable++;
            }
            tmpPawns.Clear();

            columns.Clear();
            columnsWidth = 0f;
            List<SummaryColumn> every = SummaryColumns.All;
            for (int i = 0; i < every.Count; i++)
            {
                if (!every[i].Visible) continue;
                every[i].Measure();
                columns.Add(every[i]);
                columnsWidth += every[i].width;
            }
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

            countText = shown.Count == all.Count
                ? "ColonistSummary_ColonistCount".Translate(all.Count).ToString()
                : "ColonistSummary_ShownOf".Translate(shown.Count, all.Count).ToString();

            stats.Clear();
            stats.Add(new SummaryStat(SummaryColumns.ColonistsTex, all.Count, Theme.HighlightGreen, "ColonistSummary_Stat_Colonists".Translate(), "ColonistSummary_Stat_ColonistsTip".Translate()));
            stats.Add(new SummaryStat(SummaryColumns.BleedingTex, bleeding, Theme.HighlightRed, "ColonistSummary_Stat_Bleeding".Translate(), "ColonistSummary_Stat_BleedingTip".Translate()));
            stats.Add(new SummaryStat(SummaryColumns.TemperatureTex, uncomfortable, Theme.HighlightYellow, "ColonistSummary_Stat_Uncomfortable".Translate(), "ColonistSummary_Stat_UncomfortableTip".Translate()));
            version++;
        }

        // Ties keep the colonist bar's order
        private static int CompareRows(PawnSummary a, PawnSummary b)
        {
            int result = sortBy.Compare(a, b);
            if (descending) result = -result;
            return result != 0 ? result : a.order.CompareTo(b.order);
        }
    }
}
