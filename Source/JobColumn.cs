using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // What each colonist is doing right now; hover for the full text when it's cut short
    public class JobColumn : SummaryColumn
    {
        public override bool HeadingLeft => true;

        public JobColumn()
        {
            width = 200f;
            startsDescending = false;
        }

        public override void Cell(Rect rect, PawnSummary row)
        {
            if (row.jobText.Length == 0) return;
            Rect text = new Rect(rect.x + 6f, rect.y, rect.width - 10f, rect.height);
            CoreWidgets.Line(text, row.jobText, Theme.TextBright, GameFont.Small, TextAnchor.MiddleLeft);
            CoreWidgets.Tip(rect, label, row.jobText);
        }

        public override int Compare(PawnSummary a, PawnSummary b) => string.Compare(a.jobText, b.jobText, System.StringComparison.CurrentCultureIgnoreCase);
    }
}
