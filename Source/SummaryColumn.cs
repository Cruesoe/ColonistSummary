using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // One column of a table page: heading, cell and sort order; built once per session
    public abstract class SummaryColumn
    {
        public string label = "";
        public string tip = "";
        public Texture? icon;
        public float width;
        // Numbers start sorted high to low
        public bool startsDescending = true;
        // Stable name, and the name the column-visibility setting uses (a whole block shares one)
        public string key = "";
        public string hideKey = "";

        // Whether the game has what the column shows (a DLC, a setting); hidden columns stay available
        public virtual bool Available => true;

        // Heading starts at the left, for columns whose cells start at the left
        public virtual bool HeadingLeft => false;

        // The DLC the column needs, for the settings page; null when it needs none or it's active
        public virtual string? Requirement => null;

        public bool Shown => Available && !ColonistSummaryMod.Settings.IsHidden(hideKey);

        // Sets width from the listed rows, after each rebuild
        public virtual void Measure() { }

        public abstract void Cell(Rect rect, PawnSummary row);

        public abstract int Compare(PawnSummary a, PawnSummary b);
    }
}
