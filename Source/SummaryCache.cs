using System.Collections.Generic;
using Verse;

namespace ColonySummary
{
    // Per-pawn row data; lifetime is the Summary window, cleared when it opens and closes
    public static class SummaryCache
    {
        private static readonly Dictionary<Pawn, PawnSummary> entries = new Dictionary<Pawn, PawnSummary>();

        public static PawnSummary Get(Pawn pawn)
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
            return summary;
        }

        public static void Clear() => entries.Clear();
    }
}
