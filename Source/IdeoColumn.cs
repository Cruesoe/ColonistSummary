using RimWorld;
using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // Ideoligion icon in its color; click to open it in the Ideoligions tab. Shown with Ideology outside classic mode
    public class IdeoColumn : SummaryColumn
    {
        private const float IconSize = 22f;

        public IdeoColumn()
        {
            width = 34f;
        }

        public override bool Visible => ModsConfig.IdeologyActive && !Find.IdeoManager.classicMode;

        public override void Cell(Rect rect, PawnSummary row)
        {
            Ideo? ideo = row.pawn.Ideo;
            if (ideo == null) return;
            Rect icon = new Rect(Mathf.Round(rect.center.x - IconSize / 2f), Mathf.Round(rect.center.y - IconSize / 2f), IconSize, IconSize);
            float hover = ThemeMotion.Hover(icon, 30411, Mouse.IsOver(icon));
            if (hover > 0f) Rounded.Fill(icon.ExpandedBy(2f), new Color(1f, 1f, 1f, 0.08f * hover));
            CoreWidgets.DrawIcon(icon, ideo.Icon, ideo.Color);
            CoreWidgets.Tip(icon, label, row.ideoTip);
            if (Widgets.ButtonInvisible(icon))
            {
                Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Ideos);
                IdeoUIUtility.OpenIdeoInfo(ideo);
            }
        }

        public override int Compare(PawnSummary a, PawnSummary b) => Value(a).CompareTo(Value(b));

        private static int Value(PawnSummary row) => row.pawn.Ideo?.id ?? int.MinValue;
    }
}
