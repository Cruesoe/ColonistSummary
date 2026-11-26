using UnityEngine;

namespace ColonistSummary
{
    // A narrow column in a run of like columns (skills, capacities), headed in UI Rethemed - Work's heading style
    public abstract class BlockColumn : SummaryColumn
    {
        // Heading icon for the icons style
        public abstract Texture HeadingIcon { get; }
    }
}
