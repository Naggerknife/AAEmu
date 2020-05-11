using System;

using AAEmu.Game.Core.Managers.UnitManagers;
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
            _log.Debug("DoodadFuncUse: skillId {0}, SkillId {1}", skillId, SkillId);

            var character = (Character)caster;
            if (character != null)
            {
                character.LaborPowerModified = DateTime.Now;
                character.ChangeLabor(-10, 0);
            }

            var func = DoodadManager.Instance.GetFunc(owner.FuncGroupId, skillId);
            if (func.NextPhase <= 0) { return; }
            owner.FuncGroupId = (uint)func.NextPhase;
            var nextfunc = DoodadManager.Instance.GetFunc(owner.FuncGroupId, 0);
            nextfunc?.Use(caster, owner, skillId);
        }
    }
}
