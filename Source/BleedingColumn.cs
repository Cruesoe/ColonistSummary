using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // Bleeding heart with the time until death by blood loss in red, or the bleed rate when it isn't lethal soon
    public class BleedingColumn : SummaryColumn
    {
        private const float HeartSize = 18f;

        public BleedingColumn()
        {
            width = 84f;
        }

        public override void Cell(Rect rect, PawnSummary row)
        {
            if (row.bleedText.Length == 0) return;
            Rect heart = new Rect(rect.x + 6f, Mathf.Round(rect.center.y - HeartSize / 2f), HeartSize, HeartSize);
            CoreWidgets.BleedingHeart(heart, row.bloodLeft, row.bleedRate, true);
            Rect text = new Rect(heart.xMax + 4f, rect.y, rect.xMax - heart.xMax - 6f, rect.height);
            CoreWidgets.Line(text, row.bleedText, row.bleedLethal ? Theme.TextRed : Theme.TextYellow, GameFont.Small, TextAnchor.MiddleLeft);
            CoreWidgets.Tip(rect, label, row.bleedTip, null, row.bleedLethal ? Theme.HighlightRed : Theme.HighlightYellow);
        }

        // Soonest death first when sorted descending, then the fastest bleeding
        public override int Compare(PawnSummary a, PawnSummary b) => Value(a).CompareTo(Value(b));

        private static float Value(PawnSummary row) => row.bleedLethal ? 1e9f - row.bleedDeathTicks : row.bleedRate;
    }
}
