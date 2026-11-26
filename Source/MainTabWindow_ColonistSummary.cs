using System;
using RimWorld;
using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // The Summary tab in the UI Rethemed look: title bar with the colony's numbers, search and sort menu, then the colonists table
    public class MainTabWindow_ColonistSummary : MainTabWindow
    {
        private const string Part = "Colonist Summary";
        private const float TitleHeight = 40f;
        private const float Pad = 6f;
        private const float MinWidth = 900f;
        private const float MinHeight = 360f;
        private const float UsualMinHeight = 560f;
        private const float MaxHeight = 900f;
        private const float HeightOfScreen = 0.8f;
        private const float MinSearchWidth = 140f;
        private const float MaxSearchWidth = 240f;
        private const float ControlHeight = 28f;
        private const string SearchControl = "ColonistSummary_Search";

        // Session lifetime, from translations only
        private static string? searchHint;
        private static string? searchTitle;
        private static string? searchTip;
        private static string? sortMenuTitle;
        private static string? failed;
        private static string? summaryLabel;
        private static string? summaryTip;
        private static string? healthLabel;
        private static string? healthTip;
        private static float summaryWidth;
        private static float healthWidth;

        private readonly Action draw;
        private readonly Action close;
        private readonly Func<SortChoices> sortChoices;
        private readonly Func<bool> isOpen;
        private Rect inner;
        private float openedAt;
        private int laidOut = -1;
        private string sortLabel = "";
        private string sortTip = "";
        private string searchFound = "";
        private SortChoices? choices;
        private int choicesKey = -1;

        public MainTabWindow_ColonistSummary()
        {
            doWindowBackground = false;
            draw = DrawContents;
            close = () => Failsafe.Later(() => Find.MainTabsRoot.EscapeCurrentTab());
            sortChoices = SortChoices;
            isOpen = () => Find.WindowStack.IsOpen(this);
        }

        protected override float Margin => 0f;

        // The usual size is as wide as the columns and 80% of the screen high, scaled or filled by the size settings;
        // with fit to colonists the height shrinks to the rows
        public override Vector2 RequestedTabSize
        {
            get
            {
                SummaryData.Refresh();
                ColonistSummarySettings settings = ColonistSummaryMod.Settings;
                float width = Mathf.Max(MinWidth, Mathf.Ceil(SummaryTable.ContentWidth + 16f + 8f + Pad * 2f));
                float height = Mathf.Clamp(UI.screenHeight * HeightOfScreen, UsualMinHeight, MaxHeight);
                Vector2 size = settings.size.Size(new Vector2(width, height));
                if (settings.fitToColonists)
                {
                    // Every colonist counts, so searching never moves the window
                    float rows = SummaryData.all.Count * (SummaryTable.RowHeight + SummaryTable.RowGap);
                    float fit = TitleHeight + Pad + SummaryTable.PanelChrome + BlockHeader.Height + SummaryTable.RowGap + rows + Pad * 2f;
                    // Room for the sideways scroll bar when the columns don't fit the width
                    if (size.x < width) fit += SummaryTable.ScrollBar;
                    size.y = Mathf.Min(size.y, Mathf.Max(MinHeight, Mathf.Ceil(fit)));
                }
                return size;
            }
        }

        // Resizes the open tab when the size settings or the rows change what it wants
        public override void WindowUpdate()
        {
            base.WindowUpdate();
            ColonistSummaryMod.Settings.size.Follow(this, RequestedTabSize);
        }

        public override void PreOpen()
        {
            SummaryData.Clear();
            BlockHeader.Opened();
            SummaryTable.Opened();
            searchHint ??= "ColonistSummary_SearchHint".Translate();
            searchTitle ??= "ColonistSummary_Search".Translate();
            searchTip ??= "ColonistSummary_SearchTip".Translate();
            sortMenuTitle ??= "ColonistSummary_SortMenu".Translate();
            failed ??= "ColonistSummary_Failed".Translate();
            summaryLabel ??= "ColonistSummary_Page_Summary".Translate();
            summaryTip ??= "ColonistSummary_Page_SummaryTip".Translate();
            healthLabel ??= "ColonistSummary_Page_Health".Translate();
            healthTip ??= "ColonistSummary_Page_HealthTip".Translate();
            summaryWidth = Mathf.Ceil(CoreWidgets.Width(summaryLabel) + 44f);
            healthWidth = Mathf.Ceil(CoreWidgets.Width(healthLabel) + 44f);
            laidOut = -1;
            choices = null;
            base.PreOpen();
        }

        public override void PostOpen()
        {
            base.PostOpen();
            openedAt = ThemeMotion.Now;
        }

        public override void PostClose()
        {
            base.PostClose();
            SummaryData.Clear();
        }

        public override void DoWindowContents(Rect inRect)
        {
            SummaryData.Refresh();
            if (laidOut != SummaryData.version) Relabel();
            inner = inRect;
            if (Failsafe.Failed(Part))
            {
                Rounded.WindowBackground(inRect, Corners.TopRight);
                CoreWidgets.Empty(inRect, failed);
                return;
            }
            try
            {
                Failsafe.Run(Part, draw);
            }
            finally
            {
                CoreWidgets.HoldTips = false;
                Theme.CloseScrolls();
                Theme.Reset();
            }
        }

        // Strings that change only with the rows, the sort or the search
        private void Relabel()
        {
            laidOut = SummaryData.version;
            sortLabel = SummaryData.sortBy.label;
            string direction = SummaryData.descending ? "UIRethemedCore_Descending".Translate() : "UIRethemedCore_Ascending".Translate();
            sortTip = "ColonistSummary_SortButtonTip".Translate(sortLabel, direction);
            searchFound = "ColonistSummary_SearchFound".Translate(SummaryData.shown.Count);
        }

        private float Age => ThemeMotion.On ? ThemeMotion.Now - openedAt : float.MaxValue;

        private void DrawContents()
        {
            SearchField.StopOnClickAway();
            Rounded.WindowBackground(inner, Corners.TopRight);
            Rect rect = inner.ContractedBy(Pad);
            Rect titleBar = new Rect(rect.x, rect.y, rect.width, TitleHeight);
            TitleBar(titleBar);
            SummaryTable.Draw(new Rect(rect.x, titleBar.yMax + Pad, rect.width, rect.yMax - titleBar.yMax - Pad));
        }

        private void TitleBar(Rect rect)
        {
            Rect room = CoreWidgets.TitleBar(rect, SummaryColumns.TitleTex, def.LabelCap, Age, close, ColonistSummaryMod.Instance);
            float y = Mathf.Round(rect.center.y - ControlHeight / 2f);

            float sortWidth = SortMenu.ButtonWidth(sortLabel);
            Rect sort = new Rect(room.xMax - sortWidth, y, sortWidth, ControlHeight);
            SortMenu.Button(sort, new Rect(windowRect.position + sort.position, sort.size), sortLabel, SummaryData.descending, sortTip, sortChoices, isOpen);

            float searchWidth = Mathf.Clamp(sort.x - 4f - room.x - 32f, MinSearchWidth, MaxSearchWidth);
            Rect search = new Rect(sort.x - 4f - searchWidth, y, searchWidth, ControlHeight);
            if (SearchField.Draw(search, ref SummaryData.search, SearchControl, searchHint, searchFound))
            {
                SummaryData.SetDirty();
            }
            CoreWidgets.Tip(search, searchTitle, searchTip);

            float x = PageButton(room.x + 16f, y, summaryWidth, SummaryPage.Summary, summaryLabel!, SummaryColumns.ColonistsTex, summaryTip!);
            x = PageButton(x + 4f, y, healthWidth, SummaryPage.Health, healthLabel!, SummaryColumns.HealthTex, healthTip!);
            CoreWidgets.SummaryStrip(new Rect(x + 12f, y, search.x - 8f - x - 12f, ControlHeight), SummaryData.stats);
        }

        // One page switch; returns its right edge
        private static float PageButton(float x, float y, float width, SummaryPage page, string label, Texture2D icon, string tip)
        {
            bool on = SummaryData.page == page;
            if (CoreWidgets.Button(new Rect(x, y, width, ControlHeight), label, icon, Theme.HighlightGreen, true, tip, GameFont.Small, label, on) && !on)
            {
                SummaryData.SetPage(page);
            }
            return x + width;
        }

        // Sort menu choices, read by the menu every frame; rebuilt when the page or its shown columns change
        private static int ChoicesKey => (int)SummaryData.page * 10000 + SummaryData.columns.Count;

        private SortChoices SortChoices()
        {
            if (choices == null || choicesKey != ChoicesKey)
            {
                choices = BuildChoices();
                choicesKey = ChoicesKey;
            }
            choices.sortBy = SummaryData.sortBy;
            choices.descending = SummaryData.descending;
            return choices;
        }

        private SortChoices BuildChoices()
        {
            SortChoices choices = new SortChoices
            {
                title = sortMenuTitle,
                sortBy = SummaryData.sortBy,
                descending = SummaryData.descending,
                pickSort = key =>
                {
                    if (key is SummaryColumn column && column != SummaryData.sortBy)
                    {
                        SummaryData.sortBy = column;
                        SummaryData.descending = column.startsDescending;
                        SummaryData.SetDirty();
                    }
                },
                setDescending = value =>
                {
                    SummaryData.descending = value;
                    SummaryData.SetDirty();
                }
            };
            Add(choices, SummaryColumns.Usual, Theme.HighlightGray);
            Add(choices, SummaryColumns.Name, Theme.HighlightGray);
            for (int i = 0; i < SummaryData.columns.Count; i++)
            {
                SummaryColumn column = SummaryData.columns[i];
                Add(choices, column, column is BlockColumn ? Theme.HighlightYellow : Theme.HighlightGreen);
            }
            return choices;
        }

        private static void Add(SortChoices choices, SummaryColumn column, Color accent)
        {
            choices.sorts.Add(new SortOption(column, column.label, column.icon, accent, column.tip));
        }
    }
}
