using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // Every column of each page in table order, with one per skill and per capacity; session lifetime, from defs and translations only
    [StaticConstructorOnStartup]
    public static class SummaryColumns
    {
        public const string SkillsKey = "Skills";
        public const string CapacitiesKey = "Capacities";

        public static readonly UsualOrderColumn Usual = new UsualOrderColumn();
        public static readonly NameColumn Name = new NameColumn();
        public static readonly List<SummaryColumn> SummaryPage = new List<SummaryColumn>();
        public static readonly List<SummaryColumn> HealthPage = new List<SummaryColumn>();
        public static readonly List<PawnCapacityDef> Capacities = new List<PawnCapacityDef>();
        // Hideable entries for the settings page in table order: a single column, or a whole block of skills or capacities
        public static readonly List<SummaryColumn> SummaryChoices = new List<SummaryColumn>();
        public static readonly List<SummaryColumn> HealthChoices = new List<SummaryColumn>();

        public static readonly Texture2D TitleTex = CoreWidgets.Icon("clipboard-text");
        public static readonly Texture2D HealthTex = CoreWidgets.Icon("heartbeat");
        public static readonly Texture2D ColonistsTex = CoreWidgets.Icon("users");
        public static readonly Texture2D BleedingTex = CoreWidgets.Icon("drop");
        public static readonly Texture2D TemperatureTex = CoreWidgets.Icon("thermometer");
        public static readonly Texture2D UsualTex = CoreWidgets.Icon("list-numbers");
        public static readonly Texture2D NameTex = CoreWidgets.Icon("identification-card");
        public static readonly Texture2D SkillTex = CoreWidgets.Icon("graduation-cap");
        public static readonly Texture2D CapacityTex = CoreWidgets.Icon("heart");
        public static readonly Texture2D WeaponTex = CoreWidgets.Icon("sword");
        public static readonly Texture2D ApparelTex = CoreWidgets.Icon("t-shirt");
        public static readonly Texture2D ArmorTex = CoreWidgets.Icon("shield");
        public static readonly Texture2D AgeTex = CoreWidgets.Icon("hourglass");
        public static readonly Texture2D DownedTex = CoreWidgets.Icon("person-arms-spread");
        public static readonly Texture2D TendingTex = CoreWidgets.Icon("first-aid");
        public static readonly Texture2D OperationsTex = CoreWidgets.Icon("syringe");
        public static readonly Texture2D MedicineTex = CoreWidgets.Icon("first-aid-kit");
        public static readonly Texture2D MentalTex = CoreWidgets.Icon("smiley-meh");

        static SummaryColumns()
        {
            Usual.label = "ColonistSummary_Sort_Usual".Translate();
            Usual.tip = "ColonistSummary_Sort_UsualTip".Translate();
            Usual.icon = UsualTex;
            Name.label = "ColonistSummary_Name".Translate();
            Name.tip = "ColonistSummary_SortNameTip".Translate();
            Name.icon = NameTex;

            List<SummaryColumn> page = SummaryPage;
            List<SummaryColumn> choices = SummaryChoices;
            Add(page, choices, new IdeoColumn(), "Ideo", CoreWidgets.Icon("hands-praying"));
            Add(page, choices, new XenotypeColumn(), "Xenotype", CoreWidgets.Icon("dna"));
            Add(page, choices, new AgeColumn(), "Age", AgeTex);
            Add(page, choices, new HostilityColumn(), "Hostility", CoreWidgets.Icon("crosshair"));
            Add(page, choices, new JobColumn(), "Job", CoreWidgets.Icon("briefcase"));
            Add(page, choices, new MoodColumn(), "Mood", CoreWidgets.Icon("smiley"));
            Add(page, choices, new NeedColumn(row => row.food), "Food", CoreWidgets.Icon("fork-knife"));
            Add(page, choices, new NeedColumn(row => row.rest), "Rest", CoreWidgets.Icon("bed"));
            Add(page, choices, new NeedColumn(row => row.joy), "Recreation", CoreWidgets.Icon("game-controller"));
            choices.Add(Block(SkillsKey, SkillTex));
            foreach (SkillDef skill in DefDatabase<SkillDef>.AllDefs.OrderByDescending(s => s.listOrder))
            {
                page.Add(new SkillColumn(skill) { icon = SkillTex, key = "Skill_" + skill.defName, hideKey = SkillsKey });
            }
            Add(page, choices, new GearColumn(true), "Weapon", WeaponTex);
            Add(page, choices, new GearColumn(false), "Apparel", ApparelTex);
            Add(page, choices, new ArmorColumn(0), "Sharp", ArmorTex);
            Add(page, choices, new ArmorColumn(1), "Blunt", ArmorTex);
            Add(page, choices, new ArmorColumn(2), "Heat", ArmorTex);
            Add(page, choices, new ComfortColumn(), "Comfort", TemperatureTex);

            page = HealthPage;
            choices = HealthChoices;
            Add(page, choices, new HealthPercentColumn(), "Health", HealthTex);
            Add(page, choices, new PainColumn(), "Pain", CoreWidgets.Icon("lightning"));
            choices.Add(Block(CapacitiesKey, CapacityTex));
            Capacities.AddRange(DefDatabase<PawnCapacityDef>.AllDefs.Where(c => c.showOnHumanlikes || c.showOnAnimals).OrderBy(c => c.listOrder));
            for (int i = 0; i < Capacities.Count; i++)
            {
                page.Add(new CapacityColumn(Capacities[i], i) { icon = CapacityTex, key = "Capacity_" + Capacities[i].defName, hideKey = CapacitiesKey });
            }
            Add(page, choices, new BleedingColumn(), "Bleeding", BleedingTex);
            Add(page, choices, new InfectionColumn(), "Infection", CoreWidgets.Icon("virus"));
            Add(page, choices, new HediffCellColumn(h => h.tending, 130f), "Tending", TendingTex);
            Add(page, choices, new MedicalCareColumn(), "MedicalCare", MedicineTex);
            Add(page, choices, new SelfTendColumn(), "SelfTend", CoreWidgets.Icon("hand-heart"));
            Add(page, choices, new HediffCellColumn(h => h.missing, 130f), "Missing", CoreWidgets.Icon("bone"));
            Add(page, choices, new ImplantsColumn(), "Implants", CoreWidgets.Icon("gear-six"));
            Add(page, choices, new HediffCellColumn(h => h.chronic, 160f), "Chronic", CoreWidgets.Icon("pill"));
            Add(page, choices, new HediffCellColumn(h => h.conditions, 160f), "Conditions", CoreWidgets.Icon("warning"));
            Add(page, choices, new OperationsColumn(), "Operations", OperationsTex);
        }

        public static List<SummaryColumn> For(SummaryPage page) => page == ColonistSummary.SummaryPage.Health ? HealthPage : SummaryPage;

        private static void Add(List<SummaryColumn> page, List<SummaryColumn> choices, SummaryColumn column, string key, Texture2D? icon)
        {
            column.key = key;
            column.hideKey = key;
            column.label = ("ColonistSummary_Column_" + key).Translate();
            column.tip = ("ColonistSummary_Column_" + key + "Tip").Translate();
            column.icon = icon;
            page.Add(column);
            choices.Add(column);
        }

        // A settings entry standing for a whole block of columns
        private static SummaryColumn Block(string key, Texture2D icon) => new UsualOrderColumn
        {
            key = key,
            hideKey = key,
            label = ("ColonistSummary_Column_" + key).Translate(),
            tip = ("ColonistSummary_Column_" + key + "Tip").Translate(),
            icon = icon
        };
    }
}
