using UnityEngine;

namespace ColonySummary
{
    // The colonist bar's order; a sort choice only, never drawn
    public class UsualOrderColumn : SummaryColumn
    {
        public UsualOrderColumn()
        {
            startsDescending = false;
        }

        public override void Cell(Rect rect, PawnSummary row) { }

        public override int Compare(PawnSummary a, PawnSummary b) => a.order.CompareTo(b.order);
    }
}
