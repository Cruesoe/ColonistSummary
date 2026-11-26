using System.Collections.Generic;
using System.Text;
using RimWorld;
using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // One pawn's Health page data, rebuilt once a game second and when its conditions change, only while the Health page shows
    public class HealthSummary
    {
        private const int RefreshTicks = 60;

        public float healthPercent;
        public string healthText = "";
        public Danger healthDanger;
        public bool downed;
        public string selfTendTip = "";
        private bool selfTendAble;

        public float pain;
        public string painText = "";
        public Color painColor;
        public string painTip = "";

        // Indexed like SummaryColumns.Capacities; able is false where the body can never do it
        public readonly float[] capLevel;
        public readonly bool[] capAble;
        public readonly string[] capText;
        public readonly Color[] capColor;
        public readonly string[] capTip;

        public readonly HediffCell tending = new HediffCell();
        public readonly HediffCell missing = new HediffCell();
        public readonly HediffCell chronic = new HediffCell();
        public readonly HediffCell conditions = new HediffCell();
        public int untended;

        // Implants and added parts: the item each came from, for its icon (null shows the default icon)
        public readonly List<ThingDef?> implants = new List<ThingDef?>();
        public string implantTip = "";
        public string implantMore = "";

        public readonly OperationStatus operations = new OperationStatus();

        private int builtTick = int.MinValue;
        private int builtHediffs = -1;
        private int tipHediffs = -1;

        private static readonly StringBuilder sb = new StringBuilder();

        public HealthSummary()
        {
            int count = SummaryColumns.Capacities.Count;
            capLevel = new float[count];
            capAble = new bool[count];
            capText = new string[count];
            capColor = new Color[count];
            capTip = new string[count];
            for (int i = 0; i < count; i++)
            {
                capText[i] = "";
                capTip[i] = "";
            }
        }

        public void Refresh(Pawn pawn)
        {
            int now = Find.TickManager.TicksGame;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            if (now - builtTick < RefreshTicks && now >= builtTick && hediffs.Count == builtHediffs) return;
            builtTick = now;
            builtHediffs = hediffs.Count;

            downed = pawn.Downed;
            healthPercent = pawn.health.summaryHealth.SummaryHealthPercent;
            healthText = healthPercent.ToStringPercent();
            healthDanger = healthPercent < 0.4f ? Danger.Losing : healthPercent < 0.8f ? Danger.Close : Danger.Safe;

            if (pawn.RaceProps.IsFlesh)
            {
                pain = pawn.health.hediffSet.PainTotal;
                Pair<string, Color> painLabel = HealthCardUtility.GetPainLabel(pawn);
                painText = pain > 0f ? painLabel.First.CapitalizeFirst() : "";
                painColor = painLabel.Second;
                painTip = pain > 0f ? HealthCardUtility.GetPainTip(pawn) : "";
            }
            else
            {
                pain = 0f;
                painText = "";
                painTip = "";
            }

            bool canDoctor = !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Doctor);
            if (canDoctor != selfTendAble || selfTendTip.Length == 0)
            {
                selfTendAble = canDoctor;
                selfTendTip = canDoctor
                    ? "AllowSelfTendTip".Translate(Faction.OfPlayer.def.pawnsPlural, 0.7f.ToStringPercent()).CapitalizeFirst().ToString()
                    : "MessageCannotSelfTendEver".Translate(pawn.LabelShort, pawn).ToString();
            }

            RefreshCapacities(pawn, hediffs.Count);
            RefreshConditions(pawn, hediffs);
            operations.Refresh(pawn);
        }

        // Levels every refresh; tips only when a level or the conditions change
        private void RefreshCapacities(Pawn pawn, int hediffCount)
        {
            List<PawnCapacityDef> defs = SummaryColumns.Capacities;
            bool rebuildTips = hediffCount != tipHediffs;
            tipHediffs = hediffCount;
            for (int i = 0; i < defs.Count; i++)
            {
                PawnCapacityDef def = defs[i];
                bool able = !pawn.Dead && PawnCapacityUtility.BodyCanEverDoCapacity(pawn.RaceProps.body, def) && ShowsFor(pawn, def);
                float level = able ? pawn.health.capacities.GetLevel(def) : 0f;
                if (able == capAble[i] && Mathf.Abs(level - capLevel[i]) < 0.001f && !rebuildTips) continue;
                capAble[i] = able;
                capLevel[i] = level;
                capText[i] = able ? level.ToStringPercent() : "";
                capColor[i] = level > 1.001f ? Theme.TextGreen : level >= 0.999f ? Theme.TextDim : level >= 0.5f ? Theme.TextYellow : Theme.TextRed;
                capTip[i] = able ? HealthCardUtility.GetPawnCapacityTip(pawn, def) : "";
            }
        }

        private static bool ShowsFor(Pawn pawn, PawnCapacityDef def)
        {
            if (pawn.RaceProps.Humanlike) return def.showOnHumanlikes;
            if (pawn.RaceProps.Animal) return def.showOnAnimals;
            return def.showOnMechanoids;
        }

        private void RefreshConditions(Pawn pawn, List<Hediff> hediffs)
        {
            tending.Begin();
            missing.Begin();
            chronic.Begin();
            conditions.Begin();
            implants.Clear();
            untended = 0;
            sb.Clear();

            List<Hediff_MissingPart> missingParts = pawn.health.hediffSet.GetMissingPartsCommonAncestors();
            for (int i = 0; i < missingParts.Count; i++)
            {
                Hediff_MissingPart part = missingParts[i];
                string partLabel = part.Part != null ? part.Part.LabelCap : part.LabelCap;
                missing.Add(partLabel, partLabel + ": " + part.LabelCap, Danger.Losing, Mathf.Min(99f, (part.Part?.coverageAbsWithChildren ?? 0f) * 100f));
            }

            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff hediff = hediffs[i];
                if (!hediff.Visible) continue;
                string line = Line(hediff);

                if (hediff.TendableNow())
                {
                    untended++;
                    bool urgent = hediff.Bleeding || hediff.TryGetComp<HediffComp_Immunizable>() != null || hediff.IsCurrentlyLifeThreatening;
                    tending.Add(hediff.LabelBaseCap, line, urgent ? Danger.Losing : Danger.Close, urgent ? 99f : Mathf.Min(98f, hediff.Severity));
                }
                else if (hediff.TryGetComp(out HediffComp_TendDuration tend) && tend.IsTended)
                {
                    string tended = "ColonistSummary_TendedQuality".Translate(tend.tendQuality.ToStringPercent(), tend.tendTicksLeft.ToStringTicksToPeriod());
                    tending.Add("ColonistSummary_Tended".Translate().CapitalizeFirst(), line + ", " + tended, Danger.Safe, 1f - tend.tendQuality);
                }

                // Missing parts have their own column
                if (hediff is Hediff_MissingPart) continue;
                if (hediff.def.countsAsAddedPartOrImplant)
                {
                    implants.Add(hediff.def.spawnThingOnRemoved);
                    if (sb.Length > 0) sb.AppendLine();
                    sb.Append("  - ").Append(line);
                    continue;
                }
                if (hediff.def.chronic || (hediff is Hediff_Injury && hediff.IsPermanent()))
                {
                    Danger level = hediff.IsCurrentlyLifeThreatening ? Danger.Losing : Danger.Close;
                    chronic.Add(hediff.LabelBaseCap, line, level, hediff.def.chronic ? 50f + Mathf.Min(49f, hediff.Severity) : Mathf.Min(49f, hediff.Severity));
                    continue;
                }
                if (hediff is Hediff_Injury || hediff.def == HediffDefOf.BloodLoss || hediff.TryGetComp<HediffComp_Immunizable>() != null) continue;
                bool pregnant = HediffDefOf.PregnantHuman != null && hediff.def == HediffDefOf.PregnantHuman;
                if (!hediff.def.isBad && !pregnant) continue;
                Danger danger = pregnant ? Danger.Safe : hediff.IsCurrentlyLifeThreatening ? Danger.Losing : Danger.Close;
                conditions.Add(hediff.LabelBaseCap, line, danger, Mathf.Clamp(hediff.Severity * 50f, 0f, 99f));
            }

            tending.End(untended > 0 ? "ColonistSummary_TendingTip".Translate(untended).ToString() : "ColonistSummary_TendedTip".Translate().ToString());
            missing.End("ColonistSummary_MissingTip".Translate());
            chronic.End("ColonistSummary_ChronicTip".Translate());
            conditions.End("ColonistSummary_ConditionsTip".Translate());
            if (untended > 0) tending.text = "ColonistSummary_Untended".Translate(untended);
            else if (tending.count > 0) tending.text = "ColonistSummary_Tended".Translate().CapitalizeFirst();
            implantTip = implants.Count > 0 ? "ColonistSummary_ImplantsTip".Translate() + "\n" + sb : "";
            implantMore = implants.Count > ImplantsColumn.IconsShown ? "+" + (implants.Count - ImplantsColumn.IconsShown) : "";
            sb.Clear();
        }

        // Condition with its stage and body part, as the Health tab lists it
        private static string Line(Hediff hediff) => hediff.Part != null ? hediff.LabelCap + " (" + hediff.Part.Label + ")" : hediff.LabelCap;
    }
}
