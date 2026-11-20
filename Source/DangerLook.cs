using UIRethemed.Core;
using UnityEngine;

namespace ColonistSummary
{
    // Theme colors for each danger level
    public static class DangerLook
    {
        public static Color Accent(Danger danger)
        {
            switch (danger)
            {
                case Danger.Losing: return Theme.HighlightRed;
                case Danger.Close: return Theme.HighlightYellow;
                default: return Theme.HighlightGreen;
            }
        }

        public static Color Text(Danger danger)
        {
            switch (danger)
            {
                case Danger.Losing: return Theme.TextRed;
                case Danger.Close: return Theme.TextYellow;
                default: return Theme.TextGreen;
            }
        }
    }
}
