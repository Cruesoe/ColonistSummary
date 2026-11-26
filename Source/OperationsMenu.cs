using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // The Health tab's add-operation menu for a pawn, listing the same operations with vanilla's own options
    // (confirmations, implant checks, info cards); falls back to plain options if that method changes
    public static class OperationsMenu
    {
        // Resolved once per session
        private static readonly MethodInfo? generate = Resolve();

        private static MethodInfo? Resolve()
        {
            MethodInfo? method = typeof(HealthCardUtility).GetMethod("GenerateSurgeryOption", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null || method.ReturnType != typeof(FloatMenuOption)) return null;
            ParameterInfo[] p = method.GetParameters();
            bool fits = p.Length == 7 && p[0].ParameterType == typeof(Pawn) && p[1].ParameterType == typeof(Thing) && p[2].ParameterType == typeof(RecipeDef)
                && p[3].ParameterType == typeof(IEnumerable<ThingDef>) && p[4].ParameterType == typeof(AcceptanceReport) && p[5].ParameterType == typeof(int)
                && p[6].ParameterType == typeof(BodyPartRecord);
            return fits ? method : null;
        }

        public static void Open(Pawn pawn)
        {
            List<FloatMenuOption> options = Options(pawn);
            if (options.Count == 0) options.Add(new FloatMenuOption("ColonistSummary_NoOperations".Translate(), null));
            Find.WindowStack.Add(new Menu(options));
        }

        // A float menu that leaves room on its right for vanilla's ingredients popup, which otherwise gets pushed back over
        // the menu near the screen edge and flickers with it under the mouse
        private class Menu : FloatMenu
        {
            private const float InfoRoom = 330f;

            public Menu(List<FloatMenuOption> options) : base(options) { }

            protected override void SetInitialSizeAndPosition()
            {
                base.SetInitialSizeAndPosition();
                float limit = UI.screenWidth - InfoRoom;
                if (windowRect.xMax > limit) windowRect.x = Mathf.Max(0f, limit - windowRect.width);
            }
        }

        // The same recipes and filters as the vanilla Health tab's operations list
        private static List<FloatMenuOption> Options(Pawn pawn)
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            int index = 0;
            foreach (RecipeDef recipe in pawn.def.AllRecipes)
            {
                if (!recipe.AvailableNow) continue;
                AcceptanceReport report = recipe.Worker.AvailableReport(pawn);
                if (!report.Accepted && report.Reason.NullOrEmpty()) continue;
                List<ThingDef> missing = recipe.PotentiallyMissingIngredients(null, pawn.MapHeld).ToList();
                if (missing.Any(x => x.isTechHediff) || missing.Any(x => x.IsDrug) || (missing.Count > 0 && recipe.dontShowIfAnyIngredientMissing)) continue;
                if (recipe.targetsBodyPart)
                {
                    foreach (BodyPartRecord part in recipe.Worker.GetPartsToApplyOn(pawn, recipe))
                    {
                        if (recipe.AvailableOnNow(pawn, part)) list.Add(Option(pawn, recipe, missing, report, index++, part));
                    }
                }
                else if (!pawn.health.hediffSet.HasHediff(recipe.addsHediff))
                {
                    list.Add(Option(pawn, recipe, missing, report, index++, null));
                }
            }
            return list;
        }

        private static FloatMenuOption Option(Pawn pawn, RecipeDef recipe, List<ThingDef> missing, AcceptanceReport report, int index, BodyPartRecord? part)
        {
            if (generate != null)
            {
                try
                {
                    if (generate.Invoke(null, new object?[] { pawn, pawn, recipe, missing, report, index, part }) is FloatMenuOption option) return option;
                }
                catch (Exception e)
                {
                    Log.Warning("[Colonist Summary] Vanilla's operation option failed; using a plain option. " + (e.InnerException?.Message ?? e.Message));
                }
            }
            return Plain(pawn, recipe, missing, report, part);
        }

        private static FloatMenuOption Plain(Pawn pawn, RecipeDef recipe, List<ThingDef> missing, AcceptanceReport report, BodyPartRecord? part)
        {
            string label = recipe.Worker.GetLabelWhenUsedOn(pawn, part).CapitalizeFirst();
            if (part != null && !recipe.hideBodyPartNames) label += " (" + part.Label + ")";
            if (!report.Reason.NullOrEmpty()) return new FloatMenuOption(label + " (" + report.Reason + ")", null);
            if (missing.Count > 0) return new FloatMenuOption(label + " (" + string.Join(", ", missing.Select(d => "MissingMedicalBillIngredient".Translate(d.label).ToString())) + ")", null);
            return new FloatMenuOption(label, () =>
            {
                TaggedString confirmation = recipe.Worker.GetConfirmation(pawn);
                if (confirmation.NullOrEmpty()) HealthCardUtility.CreateSurgeryBill(pawn, recipe, part);
                else Find.WindowStack.Add(new Dialog_MessageBox(confirmation, "Yes".Translate(), () => HealthCardUtility.CreateSurgeryBill(pawn, recipe, part), "No".Translate()));
            });
        }
    }
}
