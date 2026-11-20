using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonySummary
{
    [StaticConstructorOnStartup]
    public class MainTabWindow_ColonySummary : MainTabWindow_PawnTable
    {
        private const float SettingsButtonSize = 24f;

        private static readonly Texture2D SettingsTex = ContentFinder<Texture2D>.Get("UI/ColonySummary/Settings");

        // Session lifetime, from translations only
        private static string? settingsTip;

        private HeaderOrientation drawnOrientation = ColonySummarySettings.HeaderOrientation;

        protected override PawnTableDef PawnTableDef => ColonySummaryDefOf.ColonySummary_Summary;

        // Same pawns as the vanilla Assign tab: free colonists on every map, in caravans and in transporters, except babies
        protected override IEnumerable<Pawn> Pawns
        {
            get
            {
                List<Pawn> all = new List<Pawn>(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists);
                List<Pawn> result = new List<Pawn>(all.Count);
                for (int i = 0; i < all.Count; i++)
                {
                    if (!all[i].DevelopmentalStage.Baby()) result.Add(all[i]);
                }
                return result;
            }
        }

        public override void PostOpen()
        {
            SummaryCache.Clear();
            base.PostOpen();
        }

        public override void DoWindowContents(Rect rect)
        {
            // Gear changed while open: rebuild column widths and sort order
            if (SummaryCache.TableStale)
            {
                SummaryCache.TableStale = false;
                SetDirty();
            }
            // Header orientation changed in the settings: rebuild header height
            if (drawnOrientation != ColonySummarySettings.HeaderOrientation)
            {
                drawnOrientation = ColonySummarySettings.HeaderOrientation;
                SetDirty();
            }
            base.DoWindowContents(rect);

            // Colony name in the top left, above the Name column
            Text.Font = GameFont.Medium;
            bool wrap = Text.WordWrap;
            Text.WordWrap = false;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width / 3f, Text.LineHeight), Faction.OfPlayer.Name);
            Text.WordWrap = wrap;
            Text.Font = GameFont.Small;

            Rect settingsRect = new Rect(rect.xMax - SettingsButtonSize, rect.y, SettingsButtonSize, SettingsButtonSize);
            TooltipHandler.TipRegion(settingsRect, settingsTip ??= "ColonySummary_OpenSettings".Translate());
            if (Widgets.ButtonImage(settingsRect, SettingsTex) && ColonySummaryMod.Instance != null)
            {
                Find.WindowStack.Add(new Dialog_ModSettings(ColonySummaryMod.Instance));
            }
        }

        public override void PostClose()
        {
            base.PostClose();
            SummaryCache.Clear();
        }
    }
}
