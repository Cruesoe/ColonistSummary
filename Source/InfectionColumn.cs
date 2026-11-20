using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // The worst disease the colonist is building immunity to, green while immunity is winning and red while the disease is
    [StaticConstructorOnStartup]
    public class InfectionColumn : SummaryColumn
    {
        private const float IconSize = 16f;

        private static readonly Texture2D VirusTex = CoreWidgets.Icon("virus");

        public InfectionColumn()
        {
            width = 120f;
        }

        public override void Cell(Rect rect, PawnSummary row)
        {
            if (row.infectionText.Length == 0) return;
            Color accent = row.infectionLosing ? Theme.HighlightRed : Theme.HighlightGreen;
            Rect icon = new Rect(rect.x + 6f, Mathf.Round(rect.center.y - IconSize / 2f), IconSize, IconSize);
            CoreWidgets.DrawIcon(icon, VirusTex, accent);
            Rect text = new Rect(icon.xMax + 5f, rect.y, rect.xMax - icon.xMax - 7f, rect.height);
            CoreWidgets.Line(text, row.infectionText, row.infectionLosing ? Theme.TextRed : Theme.TextGreen, GameFont.Small, TextAnchor.MiddleLeft);
            CoreWidgets.Tip(rect, label, row.infectionTip, null, accent);
        }

        // Losing races first when sorted descending, soonest death first, then the closest winning races
        public override int Compare(PawnSummary a, PawnSummary b) => a.infectionRank.CompareTo(b.infectionRank);
    }
}
