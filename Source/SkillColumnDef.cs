using RimWorld;

namespace ColonySummary
{
    // A Summary column for one skill, generated at startup by SkillColumnGenerator
    public class SkillColumnDef : PawnColumnDef
    {
        public SkillDef? skill;

        // With horizontal headers, every other label sits lower, as on the vanilla Work tab
        public bool moveLabelDown;
    }
}
