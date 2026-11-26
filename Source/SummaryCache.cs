using System.Collections.Generic;
using Verse;

namespace ColonistSummary
{
    // Per-pawn row data; lifetime is the window, cleared when it opens and closes
    public static class SummaryCache
    {
        private static readonly Dictionary<Pawn, PawnSummary> entries = new Dictionary<Pawn, PawnSummary>();

        // Health data is built only while the Health page shows
        public static PawnSummary Get(Pawn pawn, bool health)
        {
            int signature = PawnSummary.Signature(pawn);
            if (entries.TryGetValue(pawn, out PawnSummary? summary))
            {
                if (summary.signature != signature) summary.RebuildGear(pawn, signature);
            }
            else
            {
                summary = new PawnSummary(pawn);
                summary.RebuildGear(pawn, signature);
                entries[pawn] = summary;
            }
            summary.RefreshStats(pawn);
            if (health) summary.Health.Refresh(pawn);
            return summary;
        }

        public static void Clear() => entries.Clear();
    }
}
