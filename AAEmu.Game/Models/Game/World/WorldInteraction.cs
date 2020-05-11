using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.World
{
    public interface IWorldInteraction
    {
        void Execute(Unit caster, SkillCaster casterType, BaseUnit target, SkillCastTarget targetType, uint skillId, uint doodadId = 0, DoodadFuncTemplate objectFunc = null);
    }
}
