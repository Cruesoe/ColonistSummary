using UnityEngine;

namespace ColonistSummary
{
    // Overall health as the colonist bar sums it: green from 80%, yellow from 40%, red below
    public class HealthPercentColumn : TextColumn
    {
        public HealthPercentColumn()
        {
            width = 64f;
        }

        protected override string CellText(PawnSummary row) => row.Health.healthText;

        protected override float Value(PawnSummary row) => row.Health.healthPercent;

        protected override Color CellColor(PawnSummary row) => DangerLook.Text(row.Health.healthDanger);
    }
}
