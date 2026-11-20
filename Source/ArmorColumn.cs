using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // Overall sharp, blunt or heat armor with a gauge that fills at 100%
    public class ArmorColumn : SummaryColumn
    {
        private const float GaugeHeight = 4f;

        private readonly int index;

        public ArmorColumn(int index)
        {
            this.index = index;
            width = 62f;
        }

        public override void Cell(Rect rect, PawnSummary row)
        {
            Rect text = new Rect(rect.x, rect.y + 2f, rect.width, rect.height - GaugeHeight - 6f);
            CoreWidgets.Line(text, row.armorText[index], Theme.TextBright, GameFont.Small, TextAnchor.MiddleCenter);
            Rect gauge = new Rect(rect.x + 8f, rect.yMax - GaugeHeight - 5f, rect.width - 16f, GaugeHeight);
            CoreWidgets.Gauge(gauge, row.armor[index], Theme.HighlightBlue);
            CoreWidgets.Tip(rect, label, tip);
        }

        public override int Compare(PawnSummary a, PawnSummary b) => a.armor[index].CompareTo(b.armor[index]);
    }
}
