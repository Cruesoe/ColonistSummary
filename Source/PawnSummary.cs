using System.Collections.Generic;
using System.Text;
using RimWorld;
using UIRethemed.Core;
using RimWorld.Planet;
using UnityEngine;
using Verse.AI;
using Verse;

namespace ColonistSummary
{
    // One pawn's row data: gear icons are rebuilt when the gear changes, everything else once a game second
    public class PawnSummary
    {
        private const int StatsRefreshTicks = 60;
        // Below this bleed rate the vanilla Health tab shows no bleeding
        private const float MinBleedRate = 0.01f;

        public readonly Pawn pawn;
        public string name = "";
        public float nameWidth;
        // ", " and the pawn's title, as the vanilla Work tab shows it after the name; empty without one
        public string title = "";
        private string? titleSource;
        // Place in the colonist bar's order
        public int order;
        // Lower-case name, weapon and apparel labels for the search field
        public string searchText = "";

        public int signature;
        public readonly List<GearItem> weapons = new List<GearItem>();
        public readonly List<GearItem> apparel = new List<GearItem>();
        public string? weaponLabel;

        public readonly float[] armor = new float[3];
        public readonly string[] armorText = new string[3];
        public float comfyMin;
        public string comfyText = "";
        public bool comfyNow;
        public string comfyTip = "";

        private int statsTick = int.MinValue;
        private int statsHediffCount = -1;
        public int ageTicks;
        public string ageText = "";
        public string ageTip = "";
        public float bleedRate;
        // Ticks until death by blood loss, int.MaxValue when not bleeding or not at risk soon
        public int bleedDeathTicks = int.MaxValue;
        public string bleedText = "";
        public string bleedTip = "";
        public Danger bleedDanger;
        // Share of blood left, for the bleeding heart
        public float bloodLeft = 1f;
        public string ideoTip = "";
        public string xenotypeTip = "";
        // Worst disease the body is racing to build immunity to; empty when there is none
        public string infectionText = "";
        public string infectionTip = "";
        public Danger infectionDanger;
        public float infectionRank;
        public string jobText = "";
        // Mood as a share of the bar, -1 without a mood; break thresholds as shares too
        public float moodLevel = -1f;
        public float breakMinor;
        public float breakMajor;
        public float breakExtreme;
        public Danger moodDanger;
        public string moodText = "";
        public string moodTip = "";
        public string mentalState = "";
        public readonly NeedSummary food = new NeedSummary();
        public readonly NeedSummary rest = new NeedSummary();
        public readonly NeedSummary joy = new NeedSummary();
        // Prisoners and animals are only listed on the Health page
        public RowKind kind;
        private HealthSummary? health;
        public HealthSummary Health => health ??= new HealthSummary();
        // What the cached tips and armor were built from
        private Ideo? tipIdeo;
        private string? tipIdeoName;
        private object? tipXenotype = Unbuilt;
        private int armorSignature;
        private int armorHediffCount = -1;

        private static readonly object Unbuilt = new object();
        private static readonly List<Apparel> tmpApparel = new List<Apparel>();
        private static readonly List<Hediff> tmpWounds = new List<Hediff>();
        private static readonly StringBuilder searchBuilder = new StringBuilder();

        public PawnSummary(Pawn pawn)
        {
            this.pawn = pawn;
        }

        // Changes whenever a weapon or apparel item is added, removed or damaged
        public static int Signature(Pawn pawn)
        {
            unchecked
            {
                int hash = 17;
                if (pawn.equipment != null)
                {
                    List<ThingWithComps> list = pawn.equipment.AllEquipmentListForReading;
                    for (int i = 0; i < list.Count; i++) hash = Mix(hash, list[i]);
                }
                hash = hash * 31 + 1;
                if (pawn.apparel != null)
                {
                    List<Apparel> list = pawn.apparel.WornApparel;
                    for (int i = 0; i < list.Count; i++) hash = Mix(hash, list[i]);
                }
                return hash;
            }
        }

        private static int Mix(int hash, Thing thing)
        {
            unchecked
            {
                return ((hash * 31 + thing.thingIDNumber) * 31 + thing.HitPoints) * 31 + thing.stackCount;
            }
        }

        public void RebuildGear(Pawn pawn, int newSignature)
        {
            signature = newSignature;
            weapons.Clear();
            apparel.Clear();

            if (pawn.equipment != null)
            {
                List<ThingWithComps> list = pawn.equipment.AllEquipmentListForReading;
                for (int i = 0; i < list.Count; i++) weapons.Add(new GearItem(list[i], ItemTip(pawn, list[i])));
            }
            weaponLabel = pawn.equipment?.Primary?.LabelNoCount;

            if (pawn.apparel != null)
            {
                tmpApparel.Clear();
                tmpApparel.AddRange(pawn.apparel.WornApparel);
                // Larger coverage first, so it keeps the slot when two items share one
                tmpApparel.SortBy(a => -a.def.apparel.HumanBodyCoverage);
                for (int i = 0; i < tmpApparel.Count; i++) apparel.Add(new GearItem(tmpApparel[i], ItemTip(pawn, tmpApparel[i])));
                tmpApparel.Clear();
                ApparelSlots.Assign(apparel);
            }

            statsTick = int.MinValue;
            RebuildSearch();
        }

        private void RebuildSearch()
        {
            searchBuilder.Clear();
            searchBuilder.Append(name);
            for (int i = 0; i < weapons.Count; i++) searchBuilder.Append('\n').Append(weapons[i].thing.LabelNoCount);
            for (int i = 0; i < apparel.Count; i++) searchBuilder.Append('\n').Append(apparel[i].thing.LabelNoCount);
            searchText = searchBuilder.ToString().ToLowerInvariant();
            searchBuilder.Clear();
        }

        // Stats, age and health, rebuilt once a game second, when hediffs change and after a gear change
        public void RefreshStats(Pawn pawn)
        {
            int now = Find.TickManager.TicksGame;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            if (now - statsTick < StatsRefreshTicks && now >= statsTick && hediffs.Count == statsHediffCount) return;
            statsTick = now;
            statsHediffCount = hediffs.Count;

            string newName = pawn.LabelShortCap;
            if (newName != name)
            {
                name = newName;
                nameWidth = Mathf.Ceil(CoreWidgets.Width(name, GameFont.Small));
                RebuildSearch();
            }
            string? titleShort = pawn.Name != null && pawn.story != null && !pawn.IsSubhuman ? pawn.story.TitleShortCap : null;
            if (titleShort != titleSource)
            {
                titleSource = titleShort;
                title = titleShort.NullOrEmpty() ? "" : ", " + titleShort;
            }
            Ideo? ideo = pawn.Ideo;
            if (ideo != tipIdeo || (ideo != null && ideo.name != tipIdeoName))
            {
                tipIdeo = ideo;
                tipIdeoName = ideo?.name;
                ideoTip = ideo != null ? ideo.name + "\n\n" + "ClickForMoreInfo".Translate() : "";
            }
            object? xenotype = pawn.genes == null ? null : (object?)pawn.genes.CustomXenotype ?? pawn.genes.Xenotype;
            if (xenotype != tipXenotype)
            {
                tipXenotype = xenotype;
                xenotypeTip = pawn.genes != null ? pawn.genes.XenotypeLabelCap : "";
            }

            // Armor depends on worn apparel and body armor from health conditions
            if (signature != armorSignature || hediffs.Count != armorHediffCount)
            {
                armorSignature = signature;
                armorHediffCount = hediffs.Count;
                SetArmor(pawn, 0, StatDefOf.ArmorRating_Sharp);
                SetArmor(pawn, 1, StatDefOf.ArmorRating_Blunt);
                SetArmor(pawn, 2, StatDefOf.ArmorRating_Heat);
            }

            comfyMin = pawn.GetStatValue(StatDefOf.ComfyTemperatureMin);
            float comfyMax = pawn.GetStatValue(StatDefOf.ComfyTemperatureMax);
            comfyText = comfyMin.ToStringTemperature("F0") + " ~ " + comfyMax.ToStringTemperature("F0");
            float ambient = pawn.AmbientTemperature;
            comfyNow = ambient >= comfyMin && ambient <= comfyMax;
            comfyTip = "ColonistSummary_CurrentTemperature".Translate(ambient.ToStringTemperature("F0"));

            ageTicks = (int)(pawn.ageTracker.AgeBiologicalTicks / 2500L);
            ageText = pawn.ageTracker.AgeBiologicalYears.ToStringCached();
            ageTip = pawn.ageTracker.AgeTooltipString;

            RefreshBleeding(pawn, hediffs);
            RefreshInfection(hediffs);
            RefreshJob(pawn);
            RefreshMood(pawn);
            food.Refresh(pawn.needs?.food);
            rest.Refresh(pawn.needs?.rest);
            joy.Refresh(pawn.needs?.joy);
        }

        // What the pawn is doing, as its inspect pane says; caravan members show their caravan
        private void RefreshJob(Pawn pawn)
        {
            string job;
            if (pawn.Spawned) job = pawn.GetJobReport().CapitalizeFirst();
            else if (pawn.GetCaravan() is Caravan caravan) job = "ColonistSummary_InCaravan".Translate(caravan.LabelCap);
            else job = "";
            jobText = job;
        }

        // Mood against the break thresholds; a mental state replaces it, in red
        private void RefreshMood(Pawn pawn)
        {
            Need_Mood? mood = pawn.needs?.mood;
            mentalState = pawn.InMentalState ? pawn.MentalStateDef.LabelCap.ToString() : "";
            if (mood == null)
            {
                moodLevel = -1f;
                moodText = mentalState;
                moodTip = "";
                return;
            }
            moodLevel = mood.CurLevelPercentage;
            MentalBreaker breaker = pawn.mindState.mentalBreaker;
            breakMinor = breaker.BreakThresholdMinor;
            breakMajor = breaker.BreakThresholdMajor;
            breakExtreme = breaker.BreakThresholdExtreme;
            moodDanger = moodLevel < breakMajor ? Danger.Losing : moodLevel < breakMinor ? Danger.Close : Danger.Safe;
            moodText = mentalState.Length > 0 ? mentalState : moodLevel.ToStringPercent();
            moodTip = "ColonistSummary_MoodTip".Translate(mood.MoodString.CapitalizeFirst(), moodLevel.ToStringPercent(), breakMinor.ToStringPercent(), breakMajor.ToStringPercent(), breakExtreme.ToStringPercent());
        }

        // Green when the colonist won't bleed out soon (or is deathless), yellow within a day, red within half a day
        private void RefreshBleeding(Pawn pawn, List<Hediff> hediffs)
        {
            Hediff? bloodLoss = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss);
            bloodLeft = 1f - (bloodLoss?.Severity ?? 0f);
            bleedRate = pawn.health.hediffSet.BleedRateTotal;
            bleedDeathTicks = int.MaxValue;
            bleedDanger = Danger.None;
            bleedText = "";
            bleedTip = "";
            if (bleedRate <= MinBleedRate) return;

            // Same rate and verdict text as the vanilla Health tab
            StringBuilder tip = new StringBuilder();
            tip.Append("BleedingRate".Translate()).Append(": ").Append(bleedRate.ToStringPercent()).Append('/').Append("LetterDay".Translate()).AppendLine();
            tip.Append("ColonistSummary_BloodLeft".Translate(bloodLeft.ToStringPercent())).AppendLine();
            int ticks = HealthUtility.TicksUntilDeathDueToBloodLoss(pawn);
            if (ModsConfig.BiotechActive && pawn.genes != null && pawn.genes.HasActiveGene(GeneDefOf.Deathless))
            {
                bleedDanger = Danger.Safe;
                tip.Append("Deathless".Translate().CapitalizeFirst());
            }
            else if (ticks >= GenDate.TicksPerDay)
            {
                bleedDanger = Danger.Safe;
                tip.Append("WontBleedOutSoon".Translate().CapitalizeFirst());
            }
            else
            {
                bleedDanger = ticks < GenDate.TicksPerDay / 2 ? Danger.Losing : Danger.Close;
                bleedDeathTicks = ticks;
                tip.Append("TimeToDeath".Translate(ticks.ToStringTicksToPeriod()).CapitalizeFirst());
            }
            bleedText = bleedDanger == Danger.Safe
                ? bleedRate.ToStringPercent()
                : ticks.ToStringTicksToPeriod(allowSeconds: false, shortForm: true, canUseDecimals: false);

            // Each wound's share of the total rate, worst first
            float sum = 0f;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i].Bleeding) sum += hediffs[i].BleedRate;
            }
            if (sum > 0f)
            {
                tmpWounds.Clear();
                for (int i = 0; i < hediffs.Count; i++)
                {
                    if (hediffs[i].Bleeding && hediffs[i].Visible) tmpWounds.Add(hediffs[i]);
                }
                tmpWounds.SortByDescending(h => h.BleedRate);
                if (tmpWounds.Count > 0) tip.AppendLine().AppendLine().Append("ColonistSummary_BleedingWounds".Translate());
                for (int i = 0; i < tmpWounds.Count; i++)
                {
                    Hediff wound = tmpWounds[i];
                    string rate = (wound.BleedRate / sum * bleedRate).ToStringPercent() + "/" + "LetterDay".Translate();
                    string label = wound.Part != null ? wound.LabelCap + " (" + wound.Part.Label + ")" : wound.LabelCap;
                    tip.AppendLine().Append("  - ").Append(label).Append(": ").Append(rate);
                    if (wound.IsTended()) tip.Append(", ").Append("ColonistSummary_Tended".Translate());
                }
                tmpWounds.Clear();
            }
            bleedTip = tip.ToString();
        }

        // Immunity race for each visible disease not yet fully immune; the cell shows the worst, the tip lists them all
        private void RefreshInfection(List<Hediff> hediffs)
        {
            infectionText = "";
            infectionTip = "";
            infectionDanger = Danger.None;
            infectionRank = 0f;
            Hediff? worst = null;
            int count = 0;
            StringBuilder? tip = null;
            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff hediff = hediffs[i];
                if (!hediff.Visible || !HealthFacts.TryPrognosis(hediff, out HealthFacts.Prognosis p) || !p.immunizable || p.fullyImmune) continue;
                count++;
                Danger danger = p.outcome == HealthFacts.Outcome.Fatal ? Danger.Losing : p.outcome == HealthFacts.Outcome.Close ? Danger.Close : Danger.Safe;
                float rank = InfectionRank(p, danger);
                if (worst == null || rank > infectionRank)
                {
                    worst = hediff;
                    infectionRank = rank;
                    infectionDanger = danger;
                }
                tip ??= new StringBuilder();
                if (tip.Length > 0) tip.AppendLine().AppendLine();
                tip.Append(hediff.LabelCap).AppendLine();
                tip.Append("ColonistSummary_InfectionLevels".Translate(p.immunity.ToStringPercent(), p.Fraction(p.severity).ToStringPercent())).AppendLine();
                tip.Append(InfectionVerdict(p, danger));
                if (p.fatalUntended) tip.AppendLine().Append("ColonistSummary_InfectionKeepTended".Translate());
                else if (p.needsTending) tip.AppendLine().Append("ColonistSummary_InfectionNeedsTending".Translate());
            }
            if (worst == null) return;
            infectionText = count > 1 ? "ColonistSummary_InfectionMore".Translate(worst.LabelBaseCap, count - 1).ToString() : worst.LabelBaseCap;
            infectionTip = tip!.ToString();
        }

        // Losing races first, soonest death first; then close races, then winning ones, the narrowest first
        private static float InfectionRank(HealthFacts.Prognosis p, Danger danger)
        {
            if (danger == Danger.Losing) return 3000f - Mathf.Min(p.daysToDeath, 999f);
            float margin = float.IsInfinity(p.daysToDeath) ? 999f : Mathf.Min(p.daysToDeath - p.daysToImmune, 999f);
            return (danger == Danger.Close ? 2000f : 1000f) - margin;
        }

        private static string InfectionVerdict(HealthFacts.Prognosis p, Danger danger)
        {
            if (danger == Danger.Losing) return "ColonistSummary_InfectionLosing".Translate(Days(p.daysToDeath));
            if (float.IsInfinity(p.daysToImmune)) return "ColonistSummary_InfectionSafe".Translate();
            string key = danger == Danger.Close ? "ColonistSummary_InfectionClose" : "ColonistSummary_InfectionWinning";
            return key.Translate(Days(p.daysToImmune));
        }

        private static string Days(float days) => Mathf.RoundToInt(days * GenDate.TicksPerDay).ToStringTicksToPeriod();

        // Overall armor across body parts, as the vanilla Gear tab calculates it
        private void SetArmor(Pawn pawn, int index, StatDef stat)
        {
            float total = 0f;
            float natural = Mathf.Clamp01(pawn.GetStatValue(stat) / 2f);
            List<BodyPartRecord> parts = pawn.RaceProps.body.AllParts;
            List<Apparel>? worn = pawn.apparel?.WornApparel;
            for (int i = 0; i < parts.Count; i++)
            {
                float unblocked = 1f - natural;
                if (worn != null)
                {
                    for (int j = 0; j < worn.Count; j++)
                    {
                        if (worn[j].def.apparel.CoversBodyPart(parts[i])) unblocked *= 1f - Mathf.Clamp01(worn[j].GetStatValue(stat) / 2f);
                    }
                }
                total += parts[i].coverageAbs * (1f - unblocked);
            }
            armor[index] = Mathf.Clamp(total * 2f, 0f, 2f);
            armorText[index] = armor[index].ToStringPercent();
        }

        private static string ItemTip(Pawn pawn, Thing thing)
        {
            StringBuilder sb = new StringBuilder();
            if (thing is Apparel ap)
            {
                bool forced = pawn.outfits != null && pawn.outfits.forcedHandler.IsForced(ap);
                bool locked = pawn.apparel != null && pawn.apparel.IsLocked(ap);
                if (forced) sb.Append("ApparelForcedLower".Translate().CapitalizeFirst());
                if (locked) sb.Append(forced ? ", " : "").Append(forced ? "ApparelLockedLower".Translate() : "ApparelLockedLower".Translate().CapitalizeFirst());
                if (forced || locked) sb.AppendLine().AppendLine();
            }
            sb.Append("ColonistSummary_ClickForInfo".Translate());
            return sb.ToString();
        }
    }
}
