using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.World
{
    public interface IWorldInteraction
    {
        void Execute(Unit caster, SkillCaster casterCaster, BaseUnit target, SkillCastTarget targetCaster, uint skillId);
    }
}
