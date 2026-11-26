using System;
using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // A health cell listing conditions (tending, missing parts, chronic, other): the worst one with "+N", colored by danger, all of them on hover
    public class HediffCellColumn : SummaryColumn
    {
        private const float IconSize = 16f;

        private readonly Func<HealthSummary, HediffCell> cellOf;

        public HediffCellColumn(Func<HealthSummary, HediffCell> cellOf, float width)
        {
            this.cellOf = cellOf;
            this.width = width;
        }

        public override void Cell(Rect rect, PawnSummary row)
        {
            HediffCell cell = cellOf(row.Health);
            if (cell.text.Length == 0) return;
            Color accent = DangerLook.Accent(cell.danger);
            float x = rect.x + 6f;
            if (icon != null)
            {
                CoreWidgets.DrawIcon(new Rect(x, Mathf.Round(rect.center.y - IconSize / 2f), IconSize, IconSize), icon, accent);
                x += IconSize + 5f;
            }
            CoreWidgets.Line(new Rect(x, rect.y, rect.xMax - x - 2f, rect.height), cell.text, DangerLook.Text(cell.danger), GameFont.Small, TextAnchor.MiddleLeft);
            CoreWidgets.Tip(rect, label, cell.tip, null, accent);
        }

        public override int Compare(PawnSummary a, PawnSummary b) => cellOf(a.Health).rank.CompareTo(cellOf(b.Health).rank);
    }
}
