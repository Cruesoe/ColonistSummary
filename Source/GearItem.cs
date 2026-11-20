using Verse;

namespace ColonistSummary
{
    public class GearItem
    {
        public readonly Thing thing;
        public readonly string title;
        public readonly string tip;
        public readonly string? countText;
        // Hit points as a fraction of maximum, or -1 when undamaged or without hit points
        public readonly float condition;

        public GearItem(Thing thing, string tip)
        {
            this.thing = thing;
            this.tip = tip;
            title = thing.LabelCap;
            countText = thing.stackCount > 1 ? thing.stackCount.ToString() : null;
            condition = thing.def.useHitPoints && thing.HitPoints < thing.MaxHitPoints ? (float)thing.HitPoints / thing.MaxHitPoints : -1f;
        }
    }
}
