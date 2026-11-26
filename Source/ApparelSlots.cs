using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // Where an apparel item is worn, as a sort key running head to toe: headgear, eyes, then by body height
    // with outer layers first, utility last
    public static class ApparelSlots
    {
        private const int HeadgearBand = 1000;
        private const int EyesBand = 999;
        private const int UtilityBand = -1;

        public static long Key(ApparelProperties apparel, int repeat)
        {
            ApparelLayerDef layer = apparel.LastLayer;
            int band;
            if (layer == ApparelLayerDefOf.Overhead) band = HeadgearBand;
            else if (layer == ApparelLayerDefOf.EyeCover) band = EyesBand;
            else if (layer == ApparelLayerDefOf.Belt) band = UtilityBand;
            else band = HighestGroup(apparel.bodyPartGroups);
            long bandPart = 10000 - Mathf.Clamp(band, -9000, 9000);
            long layerPart = 65535 - (Mathf.Clamp(layer.drawOrder, -30000, 30000) + 30000);
            return (bandPart << 32) | (layerPart << 8) | (long)Mathf.Clamp(repeat, 0, 255);
        }

        // Body height of the highest group the item covers (the game's list order runs from feet up to head)
        private static int HighestGroup(List<BodyPartGroupDef>? groups)
        {
            if (groups.NullOrEmpty()) return 0;
            int highest = int.MinValue;
            for (int i = 0; i < groups!.Count; i++) highest = Mathf.Max(highest, groups[i].listOrder);
            return highest;
        }

        // Keys for a pawn's apparel, sorted; two items sharing a slot (possible with mods) take the next place
        public static void Assign(List<GearItem> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                long key = Key(items[i].thing.def.apparel, 0);
                int repeat = 0;
                while (repeat < 255 && Taken(items, i, key + repeat)) repeat++;
                items[i].slot = key + repeat;
            }
            items.SortBy(item => item.slot);
        }

        private static bool Taken(List<GearItem> items, int before, long slot)
        {
            for (int j = 0; j < before; j++)
            {
                if (items[j].slot == slot) return true;
            }
            return false;
        }
    }
}
