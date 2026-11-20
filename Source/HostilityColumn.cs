using RimWorld;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // The game's fight-or-flee button, changeable right in the table
    public class HostilityColumn : SummaryColumn
    {
        private const float ButtonSize = 24f;

        public HostilityColumn()
        {
            width = 40f;
        }

        public override void Cell(Rect rect, PawnSummary row)
        {
            Pawn pawn = row.pawn;
            if (pawn.playerSettings == null || !pawn.RaceProps.Humanlike) return;
            Rect button = new Rect(Mathf.Round(rect.center.x - ButtonSize / 2f), Mathf.Round(rect.center.y - ButtonSize / 2f), ButtonSize, ButtonSize);
            HostilityResponseModeUtility.DrawResponseButton(button, pawn, true);
        }

        public override int Compare(PawnSummary a, PawnSummary b) => Value(a).CompareTo(Value(b));

        private static int Value(PawnSummary row) => row.pawn.playerSettings == null ? int.MinValue : (int)row.pawn.playerSettings.hostilityResponse;
    }
}
