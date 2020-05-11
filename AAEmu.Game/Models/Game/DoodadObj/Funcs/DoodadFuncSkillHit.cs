using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.DoodadObj.Funcs
{
    public class DoodadFuncSkillHit : DoodadFuncTemplate
    {
        public uint SkillId { get; set; }

        public override void Use(Unit caster, Doodad owner, uint skillId)
        {
            _log.Debug("DoodadFuncSkillHit: skillId {0}, SkillId {1}", skillId, SkillId);

            var func = DoodadManager.Instance.GetFunc(owner.FuncGroupId, SkillId);
            func?.Use(caster, owner, SkillId);
        }
    }
}
