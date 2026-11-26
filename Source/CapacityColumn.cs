using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // One capacity as a percentage: dim at full, green above, yellow below, red under half; hover for what affects it
    public class CapacityColumn : BlockColumn
    {
        public readonly PawnCapacityDef capacity;
        // Place in SummaryColumns.Capacities and in each HealthSummary's arrays
        public readonly int index;
        private readonly Texture2D headingIcon;

        public CapacityColumn(PawnCapacityDef capacity, int index)
        {
            this.capacity = capacity;
            this.index = index;
            label = capacity.LabelCap;
            tip = capacity.description.NullOrEmpty() ? capacity.LabelCap.ToString() : capacity.description;
            width = 50f;
            headingIcon = CoreWidgets.Icon(IconName(capacity.defName));
        }

        public override Texture HeadingIcon => headingIcon;

        // Phosphor icons for the vanilla capacities; others use a heart
        private static string IconName(string defName)
        {
            switch (defName)
            {
                case "Consciousness": return "brain";
                case "Moving": return "person-simple-walk";
                case "Manipulation": return "hand-grabbing";
                case "Talking": return "chat-text";
                case "Eating": return "fork-knife";
                case "Sight": return "eye";
                case "Hearing": return "ear";
                case "Breathing": return "wind";
                case "BloodFiltration": return "drop";
                case "BloodPumping": return "heartbeat";
                case "Metabolism": return "flame";
                default: return "heart";
            }
        }

        public override void Cell(Rect rect, PawnSummary row)
        {
            HealthSummary health = row.Health;
            if (!health.capAble[index]) return;
            CoreWidgets.Line(rect, health.capText[index], health.capColor[index], GameFont.Small, TextAnchor.MiddleCenter);
            if (health.capTip[index].Length > 0) CoreWidgets.Tip(rect, label, health.capTip[index]);
        }

        // Capacities the body can't have sort lowest
        public override int Compare(PawnSummary a, PawnSummary b) => Value(a).CompareTo(Value(b));

        private float Value(PawnSummary row) => row.Health.capAble[index] ? row.Health.capLevel[index] : -1f;
    }
}
