namespace AAEmu.Game.Models.Game.Skills
{
    public enum SkillTargetRelationType : byte
    {
        Any = 0,
        Friendly = 1,
        Party = 2,
        Raid = 3,
        Hostile = 4,
        Others = 5,
        FriendlyForDebuff = 6,
        SiegeOffenseHqUser = 7,
        Family = 8
    }
}
