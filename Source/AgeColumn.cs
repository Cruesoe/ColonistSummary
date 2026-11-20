namespace ColonistSummary
{
    // Biological age in years; hover for the full age
    public class AgeColumn : TextColumn
    {
        public AgeColumn()
        {
            width = 52f;
        }

        protected override string CellText(PawnSummary row) => row.ageText;

        protected override float Value(PawnSummary row) => row.ageTicks;

        protected override string CellTip(PawnSummary row) => row.ageTip;
    }
}
