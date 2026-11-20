using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // Every column in table order, with one per skill in the Bio tab's order; session lifetime, from defs and translations only
    [StaticConstructorOnStartup]
    public static class SummaryColumns
    {
        public static readonly UsualOrderColumn Usual = new UsualOrderColumn();
        public static readonly NameColumn Name = new NameColumn();
        public static readonly List<SummaryColumn> All = new List<SummaryColumn>();
        public static readonly List<SkillColumn> Skills = new List<SkillColumn>();

        public static readonly Texture2D TitleTex = CoreWidgets.Icon("clipboard-text");
        public static readonly Texture2D ColonistsTex = CoreWidgets.Icon("users");
        public static readonly Texture2D BleedingTex = CoreWidgets.Icon("drop");
        public static readonly Texture2D TemperatureTex = CoreWidgets.Icon("thermometer");
        public static readonly Texture2D UsualTex = CoreWidgets.Icon("list-numbers");
        public static readonly Texture2D NameTex = CoreWidgets.Icon("identification-card");
        public static readonly Texture2D SkillTex = CoreWidgets.Icon("graduation-cap");
        public static readonly Texture2D WeaponTex = CoreWidgets.Icon("sword");
        public static readonly Texture2D ApparelTex = CoreWidgets.Icon("t-shirt");
        public static readonly Texture2D ArmorTex = CoreWidgets.Icon("shield");
        public static readonly Texture2D AgeTex = CoreWidgets.Icon("hourglass");

        static SummaryColumns()
        {
            Usual.label = "ColonistSummary_Sort_Usual".Translate();
            Usual.tip = "ColonistSummary_Sort_UsualTip".Translate();
            Usual.icon = UsualTex;
            Name.label = "ColonistSummary_Name".Translate();
            Name.tip = "ColonistSummary_SortNameTip".Translate();
            Name.icon = NameTex;

            Add(new IdeoColumn(), "Ideo", CoreWidgets.Icon("hands-praying"));
            Add(new XenotypeColumn(), "Xenotype", CoreWidgets.Icon("dna"));
            Add(new AgeColumn(), "Age", AgeTex);
            Add(new HostilityColumn(), "Hostility", CoreWidgets.Icon("crosshair"));
            Add(new BleedingColumn(), "Bleeding", BleedingTex);
            foreach (SkillDef skill in DefDatabase<SkillDef>.AllDefs.OrderByDescending(s => s.listOrder))
            {
                SkillColumn column = new SkillColumn(skill) { icon = SkillTex };
                Skills.Add(column);
                All.Add(column);
            }
            Add(new GearColumn(true), "Weapon", WeaponTex);
            Add(new GearColumn(false), "Apparel", ApparelTex);
            Add(new ArmorColumn(0), "Sharp", ArmorTex);
            Add(new ArmorColumn(1), "Blunt", ArmorTex);
            Add(new ArmorColumn(2), "Heat", ArmorTex);
            Add(new ComfortColumn(), "Comfort", TemperatureTex);
        }

        private static void Add(SummaryColumn column, string key, Texture2D? icon)
        {
            column.label = ("ColonistSummary_Column_" + key).Translate();
            column.tip = ("ColonistSummary_Column_" + key + "Tip").Translate();
            column.icon = icon;
            All.Add(column);
        }
    }
}
