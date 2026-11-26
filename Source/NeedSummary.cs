using RimWorld;
using Verse;

namespace ColonistSummary
{
    // One need's level for its bar: red under 15%, yellow under 30%; level is -1 when the pawn lacks the need
    public class NeedSummary
    {
        public float level = -1f;
        public string text = "";
        public string tip = "";
        public Danger danger;

        public void Refresh(Need? need)
        {
            if (need == null)
            {
                level = -1f;
                text = "";
                tip = "";
                return;
            }
            level = need.CurLevelPercentage;
            text = level.ToStringPercent();
            danger = level < 0.15f ? Danger.Losing : level < 0.3f ? Danger.Close : Danger.Safe;
            tip = need.GetTipString();
        }
    }
}
