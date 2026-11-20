using System.Collections.Generic;
using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonySummary
{
    // Skill column headings drawn like UI Rethemed - Work's job headings, in the style chosen there
    public static class SkillHeader
    {
        private const float LabelRow = 20f;
        private const float LabelFoot = 24f;
        private const float MaxRotatedLabel = 110f;
        private const float MinRotatedLabel = 40f;
        private const float IconSquare = 30f;
        private const int HeadSalt = 30414;

        private static readonly float SlantSin = Mathf.Sin(Mathf.PI / 3f);
        private static readonly float SlantCos = Mathf.Cos(Mathf.PI / 3f);
        private static readonly float SlantTan = Mathf.Tan(Mathf.PI / 3f);

        // Window lifetime: read when the window opens
        public static SkillHeaderStyle style;
        private static float longestLabel;
        private static float lastOverhang;

        public static void Opened()
        {
            style = WorkHeaderStyle.Read();
            longestLabel = 0f;
            List<SkillColumn> skills = SummaryColumns.Skills;
            for (int i = 0; i < skills.Count; i++)
            {
                longestLabel = Mathf.Max(longestLabel, CoreWidgets.Width(skills[i].label) + 6f);
            }
            // How far the last classic label reaches past its column
            lastOverhang = 0f;
            if (skills.Count > 0)
            {
                SkillColumn last = skills[skills.Count - 1];
                float width = Mathf.Min(CoreWidgets.Width(last.label) + 4f, last.width * 2f - 4f);
                lastOverhang = Mathf.Max(0f, Mathf.Ceil((width - last.width) / 2f));
            }
        }

        public static float Height
        {
            get
            {
                float rotated = Mathf.Clamp(longestLabel, MinRotatedLabel, MaxRotatedLabel);
                switch (style)
                {
                    case SkillHeaderStyle.Vertical: return Mathf.Round(rotated + LabelFoot + 6f);
                    case SkillHeaderStyle.Slanted: return Mathf.Round(rotated * SlantSin + LabelRow * SlantCos + LabelFoot);
                    case SkillHeaderStyle.Icons: return 56f;
                    default: return 54f;
                }
            }
        }

        // Space between the skill block and the next column, clear of the last classic label
        public static float TrailingGap => BlockGap + (style == SkillHeaderStyle.Classic ? lastOverhang : 0f);

        private const float BlockGap = 16f;

        // Returns true when clicked
        public static bool Draw(Rect cell, int index, SkillColumn column, bool sorted, bool descending)
        {
            Rect hit = new Rect(cell.x + 1f, 0f, cell.width - 2f, cell.height);
            Rect label = default;
            if (style == SkillHeaderStyle.Classic)
            {
                float width = Mathf.Min(CoreWidgets.Width(column.label) + 4f, cell.width * 2f - 4f);
                label = new Rect(Mathf.Round(cell.center.x - width / 2f), 3f + index % 2 * LabelRow, width, LabelRow);
                hit = new Rect(cell.x + 1f, label.y, cell.width - 2f, cell.height - label.y);
            }
            bool over = Mouse.IsOver(hit) || (style == SkillHeaderStyle.Classic && Mouse.IsOver(label));
            float lit = ThemeMotion.Hover(cell, HeadSalt, over);

            if (lit > 0f && style != SkillHeaderStyle.Icons)
            {
                float top = style == SkillHeaderStyle.Classic ? label.yMax : 2f;
                Rounded.Fill(new Rect(cell.x + 2f, top, cell.width - 4f, cell.yMax - top - 2f), new Color(1f, 1f, 1f, 0.06f * lit));
            }

            Color color = sorted ? Color.white : Color.Lerp(Theme.TextDim, Color.white, lit);
            float foot = cell.yMax - LabelFoot;
            switch (style)
            {
                case SkillHeaderStyle.Vertical:
                    Rotated(cell, foot, column.label, color, -90f);
                    break;
                case SkillHeaderStyle.Slanted:
                    Rotated(cell, foot, column.label, color, -60f);
                    break;
                case SkillHeaderStyle.Icons:
                    IconHeading(cell, column, sorted, lit);
                    break;
                default:
                    CoreWidgets.Line(label, column.label, color, GameFont.Small, TextAnchor.MiddleCenter);
                    float lineBottom = cell.yMax - 12f;
                    if (lineBottom > label.yMax)
                    {
                        Theme.Fill(new Rect(Mathf.Round(cell.center.x), label.yMax, 1f, lineBottom - label.yMax), Faces.Fade(Color.white, sorted ? 0.5f : 0.15f + 0.25f * lit));
                    }
                    break;
            }
            if (sorted) CoreWidgets.SortMarker(new Rect(Mathf.Round(cell.center.x) - 4f, cell.yMax - 22f, 8f, 10f), descending);

            if (!over) return false;
            CoreWidgets.Tip(hit, column.label, column.tip);
            return Widgets.ButtonInvisible(hit, false);
        }

        private static void Rotated(Rect cell, float foot, string text, Color color, float angle)
        {
            Vector2 pivot;
            Rect rect;
            if (angle < -80f)
            {
                float length = Mathf.Max(10f, foot - 4f);
                pivot = new Vector2(Mathf.Round(cell.center.x), Mathf.Round(foot - length / 2f));
                rect = new Rect(pivot.x - length / 2f, pivot.y - LabelRow / 2f, length, LabelRow);
            }
            else
            {
                float baseY = foot - 2f;
                pivot = new Vector2(Mathf.Round(cell.center.x + (cell.yMax - baseY) / SlantTan), Mathf.Round(baseY));
                rect = new Rect(pivot.x, pivot.y - LabelRow / 2f, Mathf.Max(10f, (foot - 4f) / SlantSin), LabelRow);
            }
            Matrix4x4 matrix = GUI.matrix;
            try
            {
                UI.RotateAroundPivot(angle, pivot);
                CoreWidgets.Line(rect, text, color, GameFont.Small, TextAnchor.MiddleLeft);
            }
            finally
            {
                GUI.matrix = matrix;
            }
        }

        private static void IconHeading(Rect cell, SkillColumn column, bool sorted, float lit)
        {
            float size = Mathf.Min(IconSquare, cell.width - 4f);
            Rect square = new Rect(Mathf.Round(cell.center.x - size / 2f), 4f, size, size);
            Rounded.Fill(square, Theme.BG);
            Rounded.Border(square, Color.Lerp(sorted ? Color.white : Theme.BGLight, Color.white, 0.5f * lit));
            GUI.color = sorted ? Color.white : Color.Lerp(Theme.TextDim, Color.white, lit);
            CoreWidgets.CenteredIcon(square, SkillRow.Icon(column.skill), Mathf.Round(size * 0.65f));
            GUI.color = Color.white;
        }
    }
}
