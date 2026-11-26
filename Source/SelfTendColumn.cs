using RimWorld;
using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // Allow self-tend, as on the vanilla Health tab: greyed out for colonists who can never doctor, with the same warning
    // when they're allowed but not assigned to doctoring
    public class SelfTendColumn : SummaryColumn
    {
        private const float TickSize = 22f;

        public SelfTendColumn()
        {
            width = 52f;
        }

        private static bool Shows(Pawn pawn) => pawn.IsColonist && !pawn.Dead && !pawn.DevelopmentalStage.Baby() && pawn.playerSettings != null;

        public override void Cell(Rect rect, PawnSummary row)
        {
            Pawn pawn = row.pawn;
            if (!Shows(pawn)) return;
            bool able = !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Doctor);
            Rect tick = new Rect(Mathf.Round(rect.center.x - TickSize / 2f), Mathf.Round(rect.center.y - TickSize / 2f), TickSize, TickSize);
            bool? clicked = CoreWidgets.Tick(tick, this, pawn.playerSettings.selfTend, Theme.HighlightGreen, able, false, false, -1f, label, row.Health.selfTendTip);
            if (clicked == null || clicked.Value == pawn.playerSettings.selfTend) return;
            pawn.playerSettings.selfTend = clicked.Value;
            if (clicked.Value && pawn.workSettings != null && pawn.workSettings.GetPriority(WorkTypeDefOf.Doctor) == 0)
            {
                Messages.Message("MessageSelfTendUnsatisfied".Translate(pawn.LabelShort, pawn), MessageTypeDefOf.CautionInput, false);
            }
        }

        public override int Compare(PawnSummary a, PawnSummary b) => Value(a).CompareTo(Value(b));

        private static int Value(PawnSummary row) => !Shows(row.pawn) ? -1 : row.pawn.playerSettings.selfTend ? 1 : 0;
    }
}
