using UIRethemed.Core;
using UnityEngine;

namespace ColonySummary
{
    // Comfortable temperature range, in green while the pawn's current temperature is inside it
    public class ComfortColumn : TextColumn
    {
        public ComfortColumn()
        {
            width = 124f;
        }

        protected override string CellText(PawnSummary row) => row.comfyText;

        // Coldest-tolerant first when sorted descending
        protected override float Value(PawnSummary row) => -row.comfyMin;

        protected override Color CellColor(PawnSummary row) => row.comfyNow ? Theme.TextGreen : Theme.TextBright;

        protected override string CellTip(PawnSummary row) => row.comfyTip;
    }
}
