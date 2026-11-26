using System.Collections.Generic;
using UIRethemed.Core;
using Verse;

namespace ColonistSummary
{
    // Window size, as the UI Rethemed tabs offer it, whether the tab shrinks to its rows, hidden columns and who the Health page lists
    public class ColonistSummarySettings : ModSettings
    {
        public readonly TabSize size = new TabSize(new FloatRange(0.7f, 1.3f), new FloatRange(0.8f, 1.2f), true, true);
        public bool fitToColonists = true;
        public bool healthPrisoners = true;
        public bool healthAnimals;
        // Column keys (or block keys) the player has hidden
        private List<string> hidden = new List<string>();
        private readonly HashSet<string> hiddenSet = new HashSet<string>();

        public bool IsHidden(string key) => hiddenSet.Contains(key);

        public bool AnyHidden => hiddenSet.Count > 0;

        public void SetHidden(string key, bool value)
        {
            if (value ? !hiddenSet.Add(key) : !hiddenSet.Remove(key)) return;
            hidden.Clear();
            hidden.AddRange(hiddenSet);
            SummaryData.SetDirty();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            size.Look();
            Scribe_Values.Look(ref fitToColonists, "fitToColonists", true);
            Scribe_Values.Look(ref healthPrisoners, "healthPrisoners", true);
            Scribe_Values.Look(ref healthAnimals, "healthAnimals", false);
            Scribe_Collections.Look(ref hidden, "hiddenColumns", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                hidden ??= new List<string>();
                hiddenSet.Clear();
                hiddenSet.UnionWith(hidden);
            }
        }

        public void ResetWindow()
        {
            size.Reset();
            fitToColonists = true;
        }

        public void ResetColumns()
        {
            hidden.Clear();
            hiddenSet.Clear();
            healthPrisoners = true;
            healthAnimals = false;
            SummaryData.SetDirty();
        }
    }
}
