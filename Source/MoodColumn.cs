using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // Mood bar with the minor, major and extreme break thresholds marked: green above minor, yellow above major, red below;
    // a mental state shows instead, in red
    public class MoodColumn : SummaryColumn
    {
        private const float BarHeight = 8f;
        private const float TextWidth = 40f;

        public MoodColumn()
        {
            width = 130f;
        }

        public override void Cell(Rect rect, PawnSummary row)
        {
            if (row.moodLevel < 0f && row.mentalState.Length == 0) return;
            Rect inner = new Rect(rect.x + 6f, rect.y, rect.width - 12f, rect.height);
            if (row.mentalState.Length > 0)
            {
                CoreWidgets.Line(inner, row.mentalState, Theme.TextRed, GameFont.Small, TextAnchor.MiddleLeft);
                if (row.moodTip.Length > 0) CoreWidgets.Tip(rect, label, row.moodTip, null, Theme.HighlightRed);
                return;
            }
            Color accent = DangerLook.Accent(row.moodDanger);
            Rect text = new Rect(inner.x, rect.y, TextWidth, rect.height);
            CoreWidgets.Line(text, row.moodText, DangerLook.Text(row.moodDanger), GameFont.Small, TextAnchor.MiddleLeft);
            Rect bar = new Rect(text.xMax + 4f, Mathf.Round(rect.center.y - BarHeight / 2f), inner.xMax - text.xMax - 4f, BarHeight);
            Rounded.Fill(bar, Theme.BGDark);
            Rounded.Fill(new Rect(bar.x, bar.y, Mathf.Max(2f, bar.width * row.moodLevel), bar.height), accent);
            Mark(bar, row.breakMinor);
            Mark(bar, row.breakMajor);
            Mark(bar, row.breakExtreme);
            CoreWidgets.Tip(rect, label, row.moodTip, null, accent);
        }

        private static void Mark(Rect bar, float at)
        {
            if (at <= 0f || at >= 1f) return;
            Theme.Fill(new Rect(Mathf.Round(bar.x + bar.width * at), bar.y - 2f, 1f, bar.height + 4f), Faces.Fade(Color.white, 0.6f));
        }

        // Mental states sort as the lowest mood
        public override int Compare(PawnSummary a, PawnSummary b) => Value(a).CompareTo(Value(b));

        private static float Value(PawnSummary row) => row.mentalState.Length > 0 ? -2f : row.moodLevel;
    }
}
