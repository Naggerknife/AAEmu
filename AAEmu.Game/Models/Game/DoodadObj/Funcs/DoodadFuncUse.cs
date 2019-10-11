using System;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.DoodadObj.Funcs
{
    public class DoodadFuncUse : DoodadFuncTemplate
    {
        public uint SkillId { get; set; }

        public override void Use(Unit caster, Doodad owner, uint skillId)
        {
            _log.Debug("DoodadFuncUse: SkillId {0}, skillId {1}", SkillId, skillId);

            var character = (Character)caster;
            if (character == null) return;

            character.LaborPowerModified = DateTime.Now;
            character.ChangeLabor(-10, 0);
        }
    }
}
