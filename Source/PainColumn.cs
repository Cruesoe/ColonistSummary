using UnityEngine;

namespace ColonistSummary
{
    // Pain level in the vanilla Health tab's words and colors; hover for what's causing it
    public class PainColumn : TextColumn
    {
        public PainColumn()
        {
            width = 96f;
        }

        protected override string CellText(PawnSummary row) => row.Health.painText;

        protected override float Value(PawnSummary row) => row.Health.pain;

        protected override Color CellColor(PawnSummary row) => row.Health.painColor;

        protected override string CellTip(PawnSummary row) => row.Health.painTip;
    }
}
