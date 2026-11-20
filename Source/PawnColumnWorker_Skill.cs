using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ColonySummary
{
    // One skill in a Work tab style box shaded by level, with its passion and the Bio tab's tooltip
    [StaticConstructorOnStartup]
    public class PawnColumnWorker_Skill : PawnColumnWorker
    {
        // Size of the Work tab's priority box
        private const float BoxSize = 25f;

        private static readonly Texture2D SortingTex = ContentFinder<Texture2D>.Get("UI/Icons/Sorting");
        private static readonly Texture2D SortingDescendingTex = ContentFinder<Texture2D>.Get("UI/Icons/SortingDescending");
        private static readonly Color DisabledColor = new Color(1f, 1f, 1f, 0.5f);

        // Vanilla Work tab header height with horizontal labels
        private const int HorizontalHeaderHeight = 50;

        // Header height that fits the longest skill label when rotated, as Division of Labor sizes Work tab headers; session lifetime, from defs only
        private static int rotatedHeaderHeight;

        private string? label;
        private Vector2 labelSize;
        private string? stacked;
        private Vector2 stackedSize;

        private SkillColumnDef Def => (SkillColumnDef)def;

        private string Label
        {
            get
            {
                if (label == null)
                {
                    label = Def.skill!.skillLabel.CapitalizeFirst();
                    Text.Font = GameFont.Small;
                    labelSize = Text.CalcSize(label);
                    // First four letters top to bottom, with a dot when the label is cut short
                    stacked = string.Join("\n", label.Substring(0, Mathf.Min(4, label.Length)).ToCharArray());
                    if (label.Length > 4) stacked += ".";
                    stackedSize = Text.CalcSize(stacked) * 1.1f;
                }
                return label;
            }
        }

        // Header in the chosen orientation, matching Division of Labor's Work tab; rotated labels are hit-tested in their own space
        public override void DoHeader(Rect rect, PawnTable table)
        {
            HeaderOrientation orientation = ColonySummarySettings.HeaderOrientation;
            string text = Label;
            if (orientation == HeaderOrientation.Horizontal)
            {
                DoHorizontalHeader(rect, table, text);
                return;
            }

            bool vertical = orientation != HeaderOrientation.Inclined;
            if (table.SortingBy == def)
            {
                Texture2D tex = table.SortingDescending ? SortingDescendingTex : SortingTex;
                Rect sortingRect = vertical
                    ? new Rect(rect.xMax - tex.width - 1f, rect.yMax - tex.height - 1f, tex.width, tex.height)
                    : new Rect(rect.center.x - tex.width / 2f, rect.yMax - tex.height, tex.width, tex.height);
                GUI.DrawTexture(sortingRect, tex);
            }

            Matrix4x4 originalMatrix = GUI.matrix;
            Rect labelRect;
            Matrix4x4 labelMatrix;
            switch (orientation)
            {
                case HeaderOrientation.Inclined:
                    (labelRect, labelMatrix) = HeaderLabel.DrawInclined(rect, text, labelSize.y);
                    break;
                case HeaderOrientation.Vertical:
                    (labelRect, labelMatrix) = HeaderLabel.DrawVertical(rect, text);
                    break;
                default:
                    labelRect = HeaderLabel.DrawStacked(rect, stacked!, stackedSize);
                    labelMatrix = originalMatrix;
                    break;
            }

            GUI.matrix = labelMatrix;
            bool mouseOver = labelRect.Contains(Event.current.mousePosition);
            bool clicked = Widgets.ButtonInvisible(labelRect);
            if (mouseOver && !vertical) Widgets.DrawHighlight(labelRect);
            GUI.matrix = originalMatrix;

            if (mouseOver && vertical) Widgets.DrawHighlight(rect);
            if (clicked) HeaderClicked(rect, table);
            MouseoverSounds.DoRegion(rect);
            if (mouseOver) TooltipHandler.TipRegion(new Rect(0f, 0f, UI.screenWidth, UI.screenHeight), GetHeaderTip(table));
        }

        // Vanilla Work tab header: centered label, every other column moved down, with a line down to the cells
        private void DoHorizontalHeader(Rect rect, PawnTable table, string text)
        {
            float top = rect.yMax - HorizontalHeaderHeight;
            Rect labelRect = new Rect(rect.center.x - labelSize.x / 2f, top, labelSize.x, labelSize.y);
            if (Def.moveLabelDown) labelRect.y += 20f;

            if (table.SortingBy == def)
            {
                Texture2D tex = table.SortingDescending ? SortingDescendingTex : SortingTex;
                GUI.DrawTexture(new Rect(rect.xMax - tex.width - 1f, rect.yMax - tex.height - 1f, tex.width, tex.height), tex);
            }

            MouseoverSounds.DoRegion(labelRect);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(labelRect, text);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = new Color(1f, 1f, 1f, 0.3f);
            Widgets.DrawLineVertical(labelRect.center.x, labelRect.yMax - 3f, top + HorizontalHeaderHeight - labelRect.yMax + 3f);
            Widgets.DrawLineVertical(labelRect.center.x + 1f, labelRect.yMax - 3f, top + HorizontalHeaderHeight - labelRect.yMax + 3f);
            GUI.color = Color.white;

            if (Mouse.IsOver(labelRect))
            {
                Widgets.DrawHighlight(labelRect);
                TooltipHandler.TipRegion(labelRect, GetHeaderTip(table));
            }
            if (Widgets.ButtonInvisible(labelRect)) HeaderClicked(rect, table);
        }

        public override int GetMinHeaderHeight(PawnTable table)
        {
            if (ColonySummarySettings.HeaderOrientation == HeaderOrientation.Horizontal)
            {
                return Mathf.Max(base.GetMinHeaderHeight(table), HorizontalHeaderHeight);
            }
            if (rotatedHeaderHeight == 0)
            {
                float longest = 0f;
                Text.Font = GameFont.Small;
                foreach (SkillDef skill in DefDatabase<SkillDef>.AllDefs)
                {
                    longest = Mathf.Max(longest, Text.CalcSize(skill.skillLabel.CapitalizeFirst()).x + GenUI.GapTiny * 2f);
                }
                rotatedHeaderHeight = Mathf.CeilToInt(longest + GenUI.GapTiny);
            }
            return Mathf.Max(base.GetMinHeaderHeight(table), rotatedHeaderHeight);
        }

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            SkillRecord? skill = pawn.skills?.GetSkill(Def.skill);
            if (skill == null) return;
            // Work tab priority box, centered in the cell
            Rect box = new Rect(rect.x + (rect.width - BoxSize) / 2f, rect.center.y - BoxSize / 2f, BoxSize, BoxSize);
            if (Mouse.IsOver(box))
            {
                TooltipHandler.TipRegion(box, new TipSignal(SkillTooltip.Get(pawn, skill), Def.skill!.GetHashCode() * 397945));
            }
            Text.Anchor = TextAnchor.MiddleCenter;
            if (skill.TotallyDisabled)
            {
                Text.Font = GameFont.Small;
                GUI.color = DisabledColor;
                Widgets.Label(box, "-");
            }
            else
            {
                int level = skill.GetLevel();
                DrawBoxBackground(box, level, skill.passion);
                Text.Font = GameFont.Medium;
                Widgets.Label(box.ContractedBy(-3f), level.ToStringCached());
                Text.Font = GameFont.Small;
            }
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        // Work tab box shading by level (awful below 4, bad to 14, mid to excellent at 20) and its grey passion flame at 40% opacity
        private static void DrawBoxBackground(Rect box, int level, Passion passion)
        {
            Texture2D lower;
            Texture2D upper;
            float blend;
            if (level < 4)
            {
                lower = WidgetsWork.WorkBoxBGTex_Awful;
                upper = WidgetsWork.WorkBoxBGTex_Bad;
                blend = level / 4f;
            }
            else if (level <= 14)
            {
                lower = WidgetsWork.WorkBoxBGTex_Bad;
                upper = WidgetsWork.WorkBoxBGTex_Mid;
                blend = (level - 4) / 10f;
            }
            else
            {
                lower = WidgetsWork.WorkBoxBGTex_Mid;
                upper = WidgetsWork.WorkBoxBGTex_Excellent;
                blend = (level - 14) / 6f;
            }
            GUI.color = Color.white;
            GUI.DrawTexture(box, lower);
            GUI.color = new Color(1f, 1f, 1f, blend);
            GUI.DrawTexture(box, upper);
            if (passion != Passion.None)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.4f);
                Rect icon = new Rect(box.center.x, box.center.y, box.width / 2f, box.height / 2f);
                GUI.DrawTexture(icon, passion == Passion.Major ? WidgetsWork.PassionWorkboxMajorIcon : WidgetsWork.PassionWorkboxMinorIcon);
            }
            GUI.color = Color.white;
        }

        // Work tab priority column widths (32 min, 39 optimal), capped at 39 so spare table width doesn't widen them
        public override int GetMinWidth(PawnTable table) => Mathf.Max(base.GetMinWidth(table), 32);

        public override int GetOptimalWidth(PawnTable table) => 39;

        public override int GetMaxWidth(PawnTable table) => 39;

        // Disabled skills sort lowest; equal levels sort by passion
        public override int Compare(Pawn a, Pawn b) => SortValue(a).CompareTo(SortValue(b));

        private int SortValue(Pawn pawn)
        {
            SkillRecord? skill = pawn.skills?.GetSkill(Def.skill);
            if (skill == null || skill.TotallyDisabled) return -1;
            return skill.GetLevel() * 3 + (int)skill.passion;
        }
    }
}
