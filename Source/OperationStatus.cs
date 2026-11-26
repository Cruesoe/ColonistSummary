using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace ColonistSummary
{
    // A pawn's queued operations and why the next one can't start yet, if it can't
    public class OperationStatus
    {
        public int count;
        public string text = "";
        public string tip = "";
        public Danger danger;
        // Waiting on something the player has to sort out
        public bool blocked;

        private static readonly StringBuilder sb = new StringBuilder();

        public void Refresh(Pawn pawn)
        {
            List<Bill> bills = pawn.BillStack.Bills;
            count = bills.Count;
            text = "";
            tip = "";
            danger = Danger.None;
            blocked = false;
            if (count == 0) return;

            Bill first = bills[0];
            text = count > 1 ? first.LabelCap + " +" + (count - 1) : first.LabelCap;
            string? reason = Blocker(pawn, first, out danger);
            blocked = danger == Danger.Losing;

            sb.Clear();
            sb.Append("ColonistSummary_OperationsTip".Translate());
            for (int i = 0; i < bills.Count; i++)
            {
                sb.AppendLine().Append("  ").Append(i + 1).Append(". ").Append(bills[i].LabelCap);
                if (bills[i].suspended) sb.Append(" (").Append("ColonistSummary_OpSuspended".Translate()).Append(')');
            }
            sb.AppendLine().AppendLine().Append(reason ?? "ColonistSummary_OpReady".Translate());
            tip = sb.ToString();
            sb.Clear();
        }

        // Why the next operation can't start: red when it never will as things stand, yellow when paused; null when it's ready
        private static string? Blocker(Pawn pawn, Bill bill, out Danger danger)
        {
            danger = Danger.Losing;
            if (bill.suspended)
            {
                danger = Danger.Close;
                return "ColonistSummary_OpSuspendedTip".Translate();
            }
            if (!pawn.Spawned) return "ColonistSummary_OpNotOnMap".Translate();
            Map map = pawn.Map;
            RecipeDef recipe = bill.recipe;

            if (!AnyDoctor(pawn, map, recipe))
            {
                WorkTypeDef work = recipe.requiredGiverWorkType ?? WorkTypeDefOf.Doctor;
                return recipe.skillRequirements.NullOrEmpty()
                    ? "ColonistSummary_OpNoDoctor".Translate(work.gerundLabel)
                    : "ColonistSummary_OpNoSkilledDoctor".Translate(work.gerundLabel, recipe.MinSkillString.TrimEnd());
            }
            for (int i = 0; i < recipe.ingredients.Count; i++)
            {
                IngredientCount ingredient = recipe.ingredients[i];
                if (!Available(pawn, map, bill, ingredient)) return "ColonistSummary_OpMissing".Translate(ingredient.filter.Summary);
            }
            if (RestUtility.FindPatientBedFor(pawn) == null) return "ColonistSummary_OpNoBed".Translate();
            danger = Danger.Safe;
            return null;
        }

        // A colonist other than the patient who does this work and has the skill
        private static bool AnyDoctor(Pawn patient, Map map, RecipeDef recipe)
        {
            WorkTypeDef work = recipe.requiredGiverWorkType ?? WorkTypeDefOf.Doctor;
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn doctor = colonists[i];
                if (doctor == patient || doctor.Downed || doctor.WorkTypeIsDisabled(work) || doctor.workSettings == null || !doctor.workSettings.WorkIsActive(work)) continue;
                if (recipe.PawnSatisfiesSkillRequirements(doctor)) return true;
            }
            return false;
        }

        // Enough unforbidden stock on the map of something the bill and the patient's medical care allow
        private static bool Available(Pawn pawn, Map map, Bill bill, IngredientCount ingredient)
        {
            foreach (ThingDef def in ingredient.filter.AllowedThingDefs)
            {
                if (!bill.ingredientFilter.Allows(def)) continue;
                if (def.IsMedicine && pawn.playerSettings != null && !pawn.playerSettings.medCare.AllowsMedicine(def)) continue;
                int needed = ingredient.CountRequiredOfFor(def, bill.recipe, bill);
                int have = 0;
                List<Thing> things = map.listerThings.ThingsOfDef(def);
                for (int i = 0; i < things.Count && have < needed; i++)
                {
                    if (!things[i].IsForbidden(Faction.OfPlayer)) have += things[i].stackCount;
                }
                if (have >= needed) return true;
            }
            return false;
        }
    }
}
