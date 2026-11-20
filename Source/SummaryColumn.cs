using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // One column of the Summary table: heading, cell and sort order; built once per session
    public abstract class SummaryColumn
    {
        public string label = "";
        public string tip = "";
        public Texture? icon;
        public float width;
        // Numbers start sorted high to low
        public bool startsDescending = true;

        public virtual bool Visible => true;

        // Sets width from the listed rows, after each rebuild
        public virtual void Measure() { }

        public abstract void Cell(Rect rect, PawnSummary row);

        public abstract int Compare(PawnSummary a, PawnSummary b);
    }
}
