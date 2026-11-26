using System.Collections.Generic;
using RimWorld;
using UIRethemed.Core;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ColonistSummary
{
    // The colonists panel: column headings and one row per colonist, with the name column pinned while the table scrolls sideways
    public static class SummaryTable
    {
        public const float NameWidth = 230f;
        public const float RowHeight = 34f;
        public const float RowGap = 3f;
        public const float PanelChrome = 42f;
        private const float HeadingHeight = 26f;
        private const float PortraitSize = 26f;
        private const float ScrollBar = 16f;
        private const float EndPad = 8f;
        private const int RowSalt = 30415;
        private const int NameSalt = 30416;

        // Window lifetime
        private static Vector2 scroll;
        private static Rect viewport;
        private static string? title;
        private static string? noColonists;
        private static string? noneShown;
        private static string? rowTip;

        public static float ContentWidth => NameWidth + SummaryData.columnsWidth + SkillHeader.TrailingGap + EndPad;

        public static float ContentHeight => SummaryData.shown.Count * (RowHeight + RowGap);

        public static void Opened()
        {
            scroll = Vector2.zero;
            title ??= "ColonistSummary_Colonists".Translate();
            noColonists ??= "ColonistSummary_NoColonists".Translate();
            noneShown ??= "ColonistSummary_NoneShown".Translate();
            rowTip ??= "ColonistSummary_RowTip".Translate();
        }

        private static bool InView => viewport.Contains(Event.current.mousePosition);

        public static void Draw(Rect rect)
        {
            Rect inner = CoreWidgets.Panel(rect, title, SummaryColumns.ColonistsTex, out Rect titleBar, 220f);
            CoreWidgets.Line(new Rect(titleBar.xMax - 220f, titleBar.y, 210f, titleBar.height), SummaryData.countText, Theme.TextDim, GameFont.Small, TextAnchor.MiddleRight);

            Rect header = new Rect(inner.x, inner.y, inner.width, SkillHeader.Height);
            Rect list = new Rect(inner.x, header.yMax + RowGap, inner.width, inner.yMax - header.yMax - RowGap);
            float width = Mathf.Max(ContentWidth, list.width - (ContentHeight > list.height ? ScrollBar : 0f));
            Header(header);
            List(list, width);
        }

        private static void Header(Rect rect)
        {
            Rounded.Fill(rect, Theme.BGDark);
            Widgets.BeginGroup(rect);
            try
            {
                List<SummaryColumn> columns = SummaryData.columns;
                float x = NameWidth - scroll.x;
                int skill = 0;
                for (int i = 0; i < columns.Count; i++)
                {
                    SummaryColumn column = columns[i];
                    Rect cell = new Rect(x, 0f, column.width, rect.height);
                    bool sorted = SummaryData.sortBy == column;
                    if (cell.xMax > NameWidth && cell.x < rect.width)
                    {
                        bool clicked;
                        if (column is SkillColumn skillColumn)
                        {
                            clicked = SkillHeader.Draw(cell, skill, skillColumn, sorted, SummaryData.descending);
                            if (clicked) SoundDefOf.Tick_High.PlayOneShotOnCamera();
                        }
                        else
                        {
                            Rect heading = new Rect(cell.x, rect.height - HeadingHeight - 2f, cell.width, HeadingHeight);
                            clicked = CoreWidgets.SortHeading(heading, column.label, column.icon, sorted, SummaryData.descending, column.label, column.tip);
                        }
                        if (clicked) SummaryData.SortOn(column);
                    }
                    if (column is SkillColumn)
                    {
                        skill++;
                        if (i + 1 >= columns.Count || !(columns[i + 1] is SkillColumn))
                        {
                            // Divider between the skill block and the next column
                            float gap = SkillHeader.TrailingGap;
                            Theme.Fill(new Rect(Mathf.Round(x + column.width + gap / 2f), 8f, 1f, rect.height - 16f), Faces.Fade(Theme.BGLight, 0.9f));
                            x += gap;
                        }
                    }
                    x += column.width;
                }

                Rounded.Fill(new Rect(0f, 0f, NameWidth, rect.height), Theme.BGDark, Corners.TopLeft | Corners.BottomLeft);
                if (scroll.x > 0.5f) Theme.Fill(new Rect(NameWidth - 1f, 4f, 1f, rect.height - 8f), Faces.Fade(Theme.BGLight, 0.9f));
                Rect nameHeading = new Rect(4f + PortraitSize + 4f, rect.height - HeadingHeight - 2f, 120f, HeadingHeight);
                bool nameSorted = SummaryData.sortBy == SummaryColumns.Name;
                if (CoreWidgets.SortHeading(nameHeading, SummaryColumns.Name.label, null, nameSorted, SummaryData.descending, SummaryColumns.Name.label, SummaryColumns.Name.tip, true))
                {
                    SummaryData.SortOn(SummaryColumns.Name);
                }
            }
            finally
            {
                Widgets.EndGroup();
            }
        }

        private static void List(Rect rect, float width)
        {
            List<PawnSummary> rows = SummaryData.shown;
            if (rows.Count == 0)
            {
                CoreWidgets.Empty(rect, SummaryData.all.Count == 0 ? noColonists : noneShown);
                return;
            }
            float height = rect.height - (width > rect.width ? ScrollBar : 0f);
            Rect view = new Rect(0f, 0f, width, Mathf.Max(ContentHeight, height));
            viewport = new Rect(scroll.x, scroll.y, rect.width, rect.height);
            Widgets.BeginScrollView(rect, ref scroll, view);
            try
            {
                float y = 0f;
                for (int i = 0; i < rows.Count; i++)
                {
                    Rect row = new Rect(0f, y, view.width, RowHeight);
                    if (row.yMax >= scroll.y && row.y <= scroll.y + rect.height) Row(row, rows[i]);
                    y += RowHeight + RowGap;
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        private static void Row(Rect row, PawnSummary summary)
        {
            Rect shown = new Rect(row.x + scroll.x, row.y, Mathf.Max(0f, row.width - scroll.x), row.height);
            bool over = Mouse.IsOver(shown) && InView;
            float hover = ThemeMotion.Hover(row, RowSalt, over);
            bool selected = Find.Selector.IsSelected(summary.pawn);
            Color back = selected ? Color.Lerp(Theme.BG, Theme.HighlightGreen, 0.12f) : Theme.BG;
            Face(shown, back, hover, Corners.All);

            List<SummaryColumn> columns = SummaryData.columns;
            float x = NameWidth;
            float pinEdge = scroll.x + NameWidth;
            // Cells under the pinned name or outside the scroll view still draw but take no hover or clicks
            bool live = InView && Event.current.mousePosition.x >= pinEdge;
            if (live || Event.current.type == EventType.Repaint)
            {
                bool held = CoreWidgets.HoldTips;
                if (!live) CoreWidgets.HoldTips = true;
                try
                {
                    for (int i = 0; i < columns.Count; i++)
                    {
                        SummaryColumn column = columns[i];
                        Rect cell = new Rect(x, row.y, column.width, row.height);
                        if (cell.xMax > pinEdge && cell.x < scroll.x + viewport.width) column.Cell(cell, summary);
                        if (column is SkillColumn && (i + 1 >= columns.Count || !(columns[i + 1] is SkillColumn))) x += SkillHeader.TrailingGap;
                        x += column.width;
                    }
                }
                finally
                {
                    CoreWidgets.HoldTips = held;
                }
            }

            Rect name = new Rect(scroll.x, row.y, NameWidth, row.height);
            Face(name, back, hover, Corners.TopLeft | Corners.BottomLeft);
            if (scroll.x > 0.5f) Theme.Fill(new Rect(pinEdge - 1f, row.y + 4f, 1f, row.height - 8f), Faces.Fade(Theme.BGLight, 0.9f));
            if (selected) Rounded.Border(shown, Faces.Fade(Theme.HighlightGreen, 0.8f));
            NameCell(name, summary);
        }

        private static void Face(Rect rect, Color back, float hover, Corners corners)
        {
            Rounded.BeginBatch();
            try
            {
                Rounded.Fill(rect, back, corners);
                if (hover > 0f) Rounded.Fill(rect.ContractedBy(2f), new Color(1f, 1f, 1f, 0.05f * hover), corners);
            }
            finally
            {
                Rounded.EndBatch();
            }
        }

        // Portrait and name; click to select the colonist and jump to them, as the vanilla tabs do
        private static void NameCell(Rect cell, PawnSummary summary)
        {
            Rect portrait = new Rect(cell.x + 4f, Mathf.Round(cell.center.y - PortraitSize / 2f), PortraitSize, PortraitSize);
            CoreWidgets.Portrait(portrait, summary.pawn, Theme.HighlightGreen);
            Rect label = new Rect(portrait.xMax + 8f, cell.y, cell.xMax - portrait.xMax - 14f, cell.height);
            bool over = Mouse.IsOver(cell) && InView;
            float hover = ThemeMotion.Hover(cell, NameSalt, over);
            Rect tag = TemplateTag(label, summary);
            if (tag.width > 0f) label.xMax = tag.x - 6f;
            CoreWidgets.Line(label, summary.name, Color.Lerp(Theme.TextBright, Color.white, hover), GameFont.Small, TextAnchor.MiddleLeft);
            if (!over) return;
            if (tag.width > 0f && Mouse.IsOver(tag)) CoreWidgets.Tip(tag, summary.workTag, summary.workTagTip);
            else CoreWidgets.Tip(cell, summary.name, rowTip);
            if (Widgets.ButtonInvisible(cell))
            {
                CameraJumper.TryJumpAndSelect(summary.pawn);
                if (Current.ProgramState == ProgramState.Playing && Event.current.button == 0)
                {
                    Find.MainTabsRoot.EscapeCurrentTab(false);
                }
            }
        }

        // UI Rethemed - Work's template chip at the right of the name, as on the Work tab; at most 55% of the room
        private static Rect TemplateTag(Rect label, PawnSummary summary)
        {
            if (summary.workTag.Length == 0) return default;
            float width = Mathf.Min(summary.workTagWidth, label.width * 0.55f);
            if (width < 30f) return default;
            Rect tag = new Rect(label.xMax - width, Mathf.Round(label.center.y - 10f), width, 20f);
            Color accent = summary.workTagColor;
            Rounded.BeginBatch();
            try
            {
                Rounded.Fill(tag, Color.Lerp(Theme.BG, accent, 0.3f), Corners.All);
                Rounded.Border(tag, accent, Corners.All);
            }
            finally
            {
                Rounded.EndBatch();
            }
            CoreWidgets.Line(tag.ContractedBy(4f, 0f), summary.workTag, Color.white, GameFont.Small, TextAnchor.MiddleCenter);
            return tag;
        }
    }
}
