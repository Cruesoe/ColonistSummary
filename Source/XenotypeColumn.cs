using RimWorld;
using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // Xenotype icon, shown with Biotech
    public class XenotypeColumn : SummaryColumn
    {
        private const float IconSize = 22f;

        public XenotypeColumn()
        {
            width = 34f;
            startsDescending = false;
        }

        public override bool Available => ModsConfig.BiotechActive;

        public override string? Requirement => ModsConfig.BiotechActive ? null : "Biotech";

        public override void Cell(Rect rect, PawnSummary row)
        {
            Texture2D? tex = row.pawn.genes?.XenotypeIcon;
            if (tex == null) return;
            Rect icon = new Rect(Mathf.Round(rect.center.x - IconSize / 2f), Mathf.Round(rect.center.y - IconSize / 2f), IconSize, IconSize);
            CoreWidgets.DrawIcon(icon, tex, XenotypeDef.IconColor);
            CoreWidgets.Tip(icon, label, row.xenotypeTip);
        }

        public override int Compare(PawnSummary a, PawnSummary b) => string.CompareOrdinal(a.xenotypeTip, b.xenotypeTip);
    }
}
