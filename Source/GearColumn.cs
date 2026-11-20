using System.Collections.Generic;
using RimWorld;
using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // Weapons or worn apparel as item slots; hover for the item, click for its info card
    public class GearColumn : SummaryColumn
    {
        private const float SlotSize = 28f;
        private const float Step = 31f;
        private const float BarHeight = 3f;

        private readonly bool weapons;

        public GearColumn(bool weapons)
        {
            this.weapons = weapons;
            startsDescending = !weapons;
        }

        private List<GearItem> Items(PawnSummary row) => weapons ? row.weapons : row.apparel;

        // Wide enough for every item of the pawn with the most
        public override void Measure()
        {
            int most = 1;
            List<PawnSummary> rows = SummaryData.all;
            for (int i = 0; i < rows.Count; i++) most = Mathf.Max(most, Items(rows[i]).Count);
            width = Mathf.Max(CoreWidgets.Width(label) + 24f, most * Step + 8f);
        }

        public override void Cell(Rect rect, PawnSummary row)
        {
            List<GearItem> items = Items(row);
            float y = Mathf.Round(rect.center.y - SlotSize / 2f);
            for (int i = 0; i < items.Count; i++)
            {
                Item(new Rect(rect.x + 4f + i * Step, y, SlotSize, SlotSize), items[i]);
            }
        }

        private static void Item(Rect slot, GearItem item)
        {
            bool over = Mouse.IsOver(slot);
            float hover = ThemeMotion.Hover(slot, 30413, over);
            Faces.Slot(slot, Color.Lerp(Theme.HighlightGray, Color.white, 0.4f * hover), 1f);
            Faces.SlotThing(slot, item.thing, 3f);
            if (item.condition >= 0f)
            {
                Rect bar = new Rect(slot.x + 3f, slot.yMax - BarHeight - 2f, slot.width - 6f, BarHeight);
                Rounded.Fill(bar, Theme.BGDark);
                Rounded.Fill(new Rect(bar.x, bar.y, Mathf.Max(1f, bar.width * item.condition), bar.height), item.condition < 0.5f ? Theme.HighlightRed : Theme.HighlightGray);
            }
            if (item.countText != null)
            {
                CoreWidgets.Line(new Rect(slot.x - 10f, slot.yMax - 18f, slot.width + 12f, 20f), item.countText, Theme.TextBright, GameFont.Small, TextAnchor.LowerRight);
            }
            CoreWidgets.Tip(slot, item.title, item.tip);
            if (Widgets.ButtonInvisible(slot))
            {
                Find.WindowStack.Add(new Dialog_InfoCard(item.thing));
            }
        }

        // Weapons group by primary weapon with the unarmed last; apparel sorts by the number of items worn
        public override int Compare(PawnSummary a, PawnSummary b)
        {
            if (!weapons) return a.apparel.Count.CompareTo(b.apparel.Count);
            string? la = a.weaponLabel;
            string? lb = b.weaponLabel;
            if (la == null || lb == null) return (lb == null).CompareTo(la == null);
            return string.Compare(lb, la, System.StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
