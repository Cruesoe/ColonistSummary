using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // A centered value from the pawn's cached summary, with an optional hover card, sorted by its number
    public abstract class TextColumn : SummaryColumn
    {
        protected abstract string CellText(PawnSummary row);

        protected abstract float Value(PawnSummary row);

        protected virtual Color CellColor(PawnSummary row) => Theme.TextBright;

        protected virtual string CellTip(PawnSummary row) => "";

        public override void Cell(Rect rect, PawnSummary row)
        {
            string text = CellText(row);
            if (text.Length == 0) return;
            CoreWidgets.Line(rect, text, CellColor(row), GameFont.Small, TextAnchor.MiddleCenter);
            string cellTip = CellTip(row);
            if (cellTip.Length > 0) CoreWidgets.Tip(rect, label, cellTip);
        }

        public override int Compare(PawnSummary a, PawnSummary b) => Value(a).CompareTo(Value(b));
    }
}
