using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Error;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.World.Interactions
{
    public class Error : IWorldInteraction
    {
        public void Execute(Unit caster, SkillCaster casterType, BaseUnit target, SkillCastTarget targetType,
            uint skillId, uint doodadId, DoodadFuncTemplate objectFunc)
        {
            caster.SendErrorMessage(ErrorMessageType.InvalidDoodadTarget); // TODO ...
        }
    }
}
