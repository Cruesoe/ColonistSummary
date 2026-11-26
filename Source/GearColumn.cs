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

        // Apparel slots worn by anyone listed, head to toe; rebuilt with the rows (window lifetime)
        private readonly List<long> slots = new List<long>();

        private List<GearItem> Items(PawnSummary row) => weapons ? row.weapons : row.apparel;

        // Weapons: wide enough for the pawn with the most. Apparel: one place per slot anyone wears, so slots line up
        public override void Measure()
        {
            List<PawnSummary> rows = SummaryData.all;
            int places = 1;
            if (weapons)
            {
                for (int i = 0; i < rows.Count; i++) places = Mathf.Max(places, rows[i].weapons.Count);
            }
            else
            {
                slots.Clear();
                for (int i = 0; i < rows.Count; i++)
                {
                    List<GearItem> items = rows[i].apparel;
                    for (int j = 0; j < items.Count; j++)
                    {
                        if (!slots.Contains(items[j].slot)) slots.Add(items[j].slot);
                    }
                }
                slots.Sort();
                for (int i = 0; i < rows.Count; i++)
                {
                    List<GearItem> items = rows[i].apparel;
                    for (int j = 0; j < items.Count; j++) items[j].position = slots.BinarySearch(items[j].slot);
                }
                places = Mathf.Max(1, slots.Count);
            }
            width = Mathf.Max(CoreWidgets.Width(label) + 24f, places * Step + 8f);
        }

        public override void Cell(Rect rect, PawnSummary row)
        {
            List<GearItem> items = Items(row);
            float y = Mathf.Round(rect.center.y - SlotSize / 2f);
            if (weapons)
            {
                for (int i = 0; i < items.Count; i++) Item(Place(rect, y, i), items[i]);
                return;
            }
            // Items are in slot order, so one pass fills each place with its item or an empty box
            int next = 0;
            for (int place = 0; place < slots.Count; place++)
            {
                if (next < items.Count && items[next].position == place) Item(Place(rect, y, place), items[next++]);
                else Empty(Place(rect, y, place));
            }
        }

        private static Rect Place(Rect rect, float y, int place) => new Rect(rect.x + 4f + place * Step, y, SlotSize, SlotSize);

        private static void Empty(Rect slot)
        {
            if (Event.current.type != EventType.Repaint) return;
            Rounded.Border(slot, Faces.Fade(Theme.HighlightGray, 0.25f));
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

        // Weapons sort A to Z by primary weapon with the unarmed after them; apparel sorts by the number of items worn
        public override int Compare(PawnSummary a, PawnSummary b)
        {
            if (!weapons) return a.apparel.Count.CompareTo(b.apparel.Count);
            string? la = a.weaponLabel;
            string? lb = b.weaponLabel;
            if (la == null || lb == null) return (la == null).CompareTo(lb == null);
            return string.Compare(la, lb, System.StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
