using System;
using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // One need (food, rest, recreation) as a bar with its level: red under 15%, yellow under 30%; hover for the game's need card
    public class NeedColumn : SummaryColumn
    {
        private const float BarHeight = 8f;
        private const float TextWidth = 40f;

        private readonly Func<PawnSummary, NeedSummary> needOf;

        public override bool HeadingLeft => true;

        public NeedColumn(Func<PawnSummary, NeedSummary> needOf)
        {
            this.needOf = needOf;
            width = 100f;
        }

        public override void Cell(Rect rect, PawnSummary row)
        {
            NeedSummary need = needOf(row);
            if (need.level < 0f) return;
            Color accent = DangerLook.Accent(need.danger);
            Rect inner = new Rect(rect.x + 6f, rect.y, rect.width - 12f, rect.height);
            Rect text = new Rect(inner.x, rect.y, TextWidth, rect.height);
            CoreWidgets.Line(text, need.text, DangerLook.Text(need.danger), GameFont.Small, TextAnchor.MiddleLeft);
            Rect bar = new Rect(text.xMax + 4f, Mathf.Round(rect.center.y - BarHeight / 2f), inner.xMax - text.xMax - 4f, BarHeight);
            Rounded.Fill(bar, Theme.BGDark);
            Rounded.Fill(new Rect(bar.x, bar.y, Mathf.Max(2f, bar.width * need.level), bar.height), accent);
            CoreWidgets.Tip(rect, label, need.tip, null, accent);
        }

        public override int Compare(PawnSummary a, PawnSummary b) => needOf(a).level.CompareTo(needOf(b).level);
    }
}
