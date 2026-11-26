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
        public const float ScrollBar = 16f;
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

        public static float ContentWidth => NameWidth + SummaryData.columnsWidth + BlockHeader.TrailingGap + EndPad;

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

            Rect header = new Rect(inner.x, inner.y, inner.width, BlockHeader.Height);
            Rect list = new Rect(inner.x, header.yMax + RowGap, inner.width, inner.yMax - header.yMax - RowGap);
            float width = Mathf.Max(ContentWidth, list.width - (ContentHeight > list.height ? ScrollBar : 0f));
            WheelAcross(new Rect(inner.x, header.y, inner.width, list.yMax - header.y), list, width);
            Header(header);
            List(list, width);
        }

        // The mouse wheel scrolls sideways when the rows fit the height, as on the research tree; with Shift it always does
        private static void WheelAcross(Rect area, Rect list, float width)
        {
            Event e = Event.current;
            if (e.type != EventType.ScrollWheel || !Mouse.IsOver(area)) return;
            bool down = ContentHeight > list.height - (width > list.width ? ScrollBar : 0f);
            float visible = list.width - (down ? ScrollBar : 0f);
            if (width <= visible || (down && !e.shift)) return;
            scroll.x = Mathf.Clamp(scroll.x + (e.delta.y + e.delta.x) * 20f, 0f, width - visible);
            e.Use();
        }

        // Lines along every edge of each block's columns, under the headings, as Work draws with column outlines on
        private static void BlockLines(float x, float width, float height)
        {
            List<SummaryColumn> columns = SummaryData.columns;
            float shift = BlockHeader.Slant(height);
            for (int i = 0; i < columns.Count; i++)
            {
                SummaryColumn column = columns[i];
                if (column is BlockColumn)
                {
                    Line(x);
                    if (BlockHeader.IsBlockEnd(columns, i))
                    {
                        Line(x + column.width);
                        x += BlockHeader.TrailingGap;
                    }
                }
                x += column.width;
            }

            void Line(float at)
            {
                float lx = Mathf.Round(at);
                if (lx + shift >= NameWidth && lx <= width) BlockHeader.ColumnLine(lx, height);
            }
        }

        private static void Header(Rect rect)
        {
            Rounded.Fill(rect, Theme.BGDark);
            Widgets.BeginGroup(rect);
            try
            {
                List<SummaryColumn> columns = SummaryData.columns;
                float x = NameWidth - scroll.x;
                if (BlockHeader.DrawsLines) BlockLines(x, rect.width, rect.height);
                int skill = 0;
                for (int i = 0; i < columns.Count; i++)
                {
                    SummaryColumn column = columns[i];
                    Rect cell = new Rect(x, 0f, column.width, rect.height);
                    bool sorted = SummaryData.sortBy == column;
                    if (cell.xMax > NameWidth && cell.x < rect.width)
                    {
                        bool clicked;
                        if (column is BlockColumn blockColumn)
                        {
                            clicked = BlockHeader.Draw(cell, skill, blockColumn, sorted, SummaryData.descending);
                            if (clicked) SoundDefOf.Tick_High.PlayOneShotOnCamera();
                        }
                        else
                        {
                            // Left headings start where the cells' content does
                            Rect heading = column.HeadingLeft
                                ? new Rect(cell.x + 2f, rect.height - HeadingHeight - 2f, cell.width - 2f, HeadingHeight)
                                : new Rect(cell.x, rect.height - HeadingHeight - 2f, cell.width, HeadingHeight);
                            clicked = CoreWidgets.SortHeading(heading, column.label, column.icon, sorted, SummaryData.descending, column.label, column.tip, column.HeadingLeft);
                        }
                        if (clicked) SummaryData.SortOn(column);
                    }
                    // Faint divider between neighboring headings, so each heading reads as its column's
                    if (!(column is BlockColumn) && i + 1 < columns.Count && !(columns[i + 1] is BlockColumn) && x + column.width > NameWidth)
                    {
                        Theme.Fill(new Rect(Mathf.Round(x + column.width), rect.height - HeadingHeight, 1f, HeadingHeight - 4f), Faces.Fade(Theme.BGLight, 0.6f));
                    }
                    if (column is BlockColumn)
                    {
                        skill++;
                        if (BlockHeader.IsBlockEnd(columns, i))
                        {
                            // Divider between a block and the next column
                            float gap = BlockHeader.TrailingGap;
                            Theme.Fill(new Rect(Mathf.Round(x + column.width + gap / 2f), rect.height - HeadingHeight, 1f, HeadingHeight - 4f), Faces.Fade(Theme.BGLight, 0.6f));
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
                        if (BlockHeader.IsBlockEnd(columns, i)) x += BlockHeader.TrailingGap;
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
            CoreWidgets.Portrait(portrait, summary.pawn, summary.kind == RowKind.Prisoner ? Theme.HighlightYellow : summary.kind == RowKind.Animal ? Theme.HighlightGray : Theme.HighlightGreen);
            Rect label = new Rect(portrait.xMax + 8f, cell.y, cell.xMax - portrait.xMax - 14f, cell.height);
            bool over = Mouse.IsOver(cell) && InView;
            float hover = ThemeMotion.Hover(cell, NameSalt, over);
            CoreWidgets.Line(label, summary.name, Color.Lerp(Theme.TextBright, Color.white, hover), GameFont.Small, TextAnchor.MiddleLeft);
            // The title follows the name in a dimmer color, as on the vanilla Work tab
            if (summary.title.Length > 0 && summary.nameWidth < label.width)
            {
                Rect title = new Rect(label.x + summary.nameWidth, label.y, label.width - summary.nameWidth, label.height);
                CoreWidgets.Line(title, summary.title, Theme.TextDim, GameFont.Small, TextAnchor.MiddleLeft);
            }
            if (!over) return;
            CoreWidgets.Tip(cell, summary.name, rowTip);
            if (Widgets.ButtonInvisible(cell))
            {
                CameraJumper.TryJumpAndSelect(summary.pawn);
                if (Current.ProgramState == ProgramState.Playing && Event.current.button == 0)
                {
                    Find.MainTabsRoot.EscapeCurrentTab(false);
                }
            }
        }
    }
}
