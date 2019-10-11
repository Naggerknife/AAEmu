namespace AAEmu.Game.Models.Game.Skills
{
    // by Nut
    /*
     * -SkillTargetSelection: This is the type of targeting source (source, target, location, or line).
     * For example, a skill that originates from the caster (eg. Thwart, songcraft songs, etc.)
     * would be type.Source, meteor would be type.location, etc.
     */
    public enum SkillTargetSelectionType : byte
    {
        Source = 1,
        Target = 2,
        Line = 3,
        Location = 4
    }
}
