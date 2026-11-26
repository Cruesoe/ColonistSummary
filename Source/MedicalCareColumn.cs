using RimWorld;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // The vanilla medicine button, changeable right in the table, for the same pawns the Health tab offers it to
    public class MedicalCareColumn : SummaryColumn
    {
        private const float ButtonWidth = 32f;
        private const float ButtonHeight = 26f;

        public MedicalCareColumn()
        {
            width = 84f;
        }

        public static bool Shows(Pawn pawn)
        {
            if (pawn.playerSettings == null || pawn.Dead || !pawn.RaceProps.IsFlesh) return false;
            if (pawn.IsMutant && !pawn.mutant.Def.entitledToMedicalCare) return false;
            return pawn.Faction == Faction.OfPlayer || pawn.HostFaction == Faction.OfPlayer;
        }

        public override void Cell(Rect rect, PawnSummary row)
        {
            if (!Shows(row.pawn)) return;
            Rect button = new Rect(Mathf.Round(rect.center.x - ButtonWidth / 2f), Mathf.Round(rect.center.y - ButtonHeight / 2f), ButtonWidth, ButtonHeight);
            MedicalCareUtility.MedicalCareSelectButton(button, row.pawn);
        }

        public override int Compare(PawnSummary a, PawnSummary b) => Value(a).CompareTo(Value(b));

        private static int Value(PawnSummary row) => Shows(row.pawn) ? (int)row.pawn.playerSettings.medCare : -1;
    }
}
