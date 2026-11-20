using UIRethemed.Core;
using Verse;

namespace ColonistSummary
{
    // Window size, as the UI Rethemed tabs offer it, and whether the tab shrinks to its colonists
    public class ColonistSummarySettings : ModSettings
    {
        public readonly TabSize size = new TabSize(new FloatRange(0.7f, 1.3f), new FloatRange(0.8f, 1.2f), true, true);
        public bool fitToColonists = true;

        public override void ExposeData()
        {
            base.ExposeData();
            size.Look();
            Scribe_Values.Look(ref fitToColonists, "fitToColonists", true);
        }

        public void Reset()
        {
            size.Reset();
            fitToColonists = true;
        }
    }
}
