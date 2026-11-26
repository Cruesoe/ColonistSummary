using System.Text;

namespace ColonistSummary
{
    // One health table cell built from a list of conditions: the worst one's text with "+N" for the rest, and a tip listing them all
    public class HediffCell
    {
        public string text = "";
        public string tip = "";
        public Danger danger;
        public int count;
        // Sort value: worse and more conditions rank higher
        public float rank;

        private readonly StringBuilder lines = new StringBuilder();
        private string first = "";
        private float firstRank = float.MinValue;

        public void Begin()
        {
            lines.Clear();
            count = 0;
            first = "";
            firstRank = float.MinValue;
            danger = Danger.None;
            rank = 0f;
        }

        // Adds one condition; the worst (highest rank) one names the cell
        public void Add(string label, string line, Danger level, float itemRank)
        {
            count++;
            if (lines.Length > 0) lines.AppendLine();
            lines.Append("  - ").Append(line);
            if (itemRank > firstRank)
            {
                firstRank = itemRank;
                first = label;
            }
            if (level > danger) danger = level;
        }

        // Item ranks run 0 to 99 within a danger level
        public void End(string heading)
        {
            if (count == 0)
            {
                text = "";
                tip = "";
                return;
            }
            text = count > 1 ? first + " +" + (count - 1) : first;
            tip = heading + "\n" + lines;
            rank = (int)danger * 10000f + count * 100f + firstRank;
            lines.Clear();
        }
    }
}
