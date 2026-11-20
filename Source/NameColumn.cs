using UnityEngine;

namespace ColonySummary
{
    // Sorts by name; the pinned name cell itself is drawn by SummaryTable
    public class NameColumn : SummaryColumn
    {
        public NameColumn()
        {
            startsDescending = false;
        }

        public override void Cell(Rect rect, PawnSummary row) { }

        public override int Compare(PawnSummary a, PawnSummary b) => string.Compare(a.name, b.name, System.StringComparison.CurrentCultureIgnoreCase);
    }
}
