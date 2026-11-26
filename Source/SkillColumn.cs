using RimWorld;
using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace ColonistSummary
{
    // One skill as a box tinted by level, with its passion and the theme's skill card on hover
    public class SkillColumn : BlockColumn
    {
        public override Texture HeadingIcon => SkillRow.Icon(skill);

        private const float BoxSize = 26f;
        private const float PassionSize = 12f;
        private const float MaxLevel = 20f;

        public readonly SkillDef skill;

        public SkillColumn(SkillDef skill)
        {
            this.skill = skill;
            label = skill.skillLabel.CapitalizeFirst();
            tip = skill.description;
            width = 38f;
        }

        public override void Cell(Rect rect, PawnSummary row)
        {
            SkillRecord? record = row.pawn.skills?.GetSkill(skill);
            if (record == null) return;
            Rect box = new Rect(Mathf.Round(rect.center.x - BoxSize / 2f), Mathf.Round(rect.center.y - BoxSize / 2f), BoxSize, BoxSize);
            float hover = ThemeMotion.Hover(box, 30412, Mouse.IsOver(box));
            if (record.TotallyDisabled)
            {
                Rounded.Fill(box, Faces.Fade(Theme.BGDark, 0.6f));
                CoreWidgets.Line(box, "-", Theme.TextFaint, GameFont.Small, TextAnchor.MiddleCenter);
            }
            else
            {
                int level = record.GetLevel();
                Color color = SkillRow.LevelColor(level);
                Rounded.Fill(box, Color.Lerp(Theme.BG, color, 0.15f + 0.35f * Mathf.Clamp01(level / MaxLevel)));
                Rounded.Border(box, Color.Lerp(Faces.Fade(color, 0.6f), Color.white, 0.5f * hover));
                CoreWidgets.Line(box, level.ToStringCached(), Theme.TextBright, GameFont.Small, TextAnchor.MiddleCenter);
                if (record.passion != Passion.None)
                {
                    Texture? passion = PassionLooks.Icon(record.passion);
                    if (passion != null) CoreWidgets.DrawIcon(new Rect(box.xMax - PassionSize + 3f, box.y - 4f, PassionSize, PassionSize), passion);
                }
            }
            if (!CoreWidgets.HoldTips && Mouse.IsOver(box)) SkillCard.Region(box, row.pawn.thingIDNumber * 397 + skill.index, record);
        }

        // Disabled skills sort lowest; equal levels sort by passion
        public override int Compare(PawnSummary a, PawnSummary b) => Value(a).CompareTo(Value(b));

        private int Value(PawnSummary row)
        {
            SkillRecord? record = row.pawn.skills?.GetSkill(skill);
            if (record == null || record.TotallyDisabled) return -1;
            return record.GetLevel() * 3 + (int)record.passion;
        }
    }
}
