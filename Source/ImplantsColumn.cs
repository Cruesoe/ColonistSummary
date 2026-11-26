using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // Prosthetics, bionics and implants as small item icons, with "+N" past the column's room; hover for each and its body part
    [StaticConstructorOnStartup]
    public class ImplantsColumn : SummaryColumn
    {
        private const float IconSize = 22f;
        private const float Step = 25f;
        public const int IconsShown = 4;

        private static readonly Texture2D DefaultTex = CoreWidgets.Icon("gear-six");

        public override bool HeadingLeft => true;

        public ImplantsColumn()
        {
            width = IconsShown * Step + 36f;
        }

        public override void Cell(Rect rect, PawnSummary row)
        {
            HealthSummary health = row.Health;
            int count = health.implants.Count;
            if (count == 0) return;
            float y = Mathf.Round(rect.center.y - IconSize / 2f);
            int shown = Mathf.Min(count, IconsShown);
            for (int i = 0; i < shown; i++)
            {
                Rect icon = new Rect(rect.x + 6f + i * Step, y, IconSize, IconSize);
                ThingDef? def = health.implants[i];
                if (def != null) Widgets.ThingIcon(icon, def);
                else CoreWidgets.DrawIcon(icon.ContractedBy(3f), DefaultTex, Theme.HighlightGray);
            }
            if (count > IconsShown)
            {
                Rect more = new Rect(rect.x + 6f + IconsShown * Step, rect.y, rect.xMax - rect.x - 8f - IconsShown * Step, rect.height);
                CoreWidgets.Line(more, health.implantMore, Theme.TextDim, GameFont.Small, TextAnchor.MiddleLeft);
            }
            CoreWidgets.Tip(rect, label, health.implantTip);
        }

        public override int Compare(PawnSummary a, PawnSummary b) => a.Health.implants.Count.CompareTo(b.Health.implants.Count);
    }
}
