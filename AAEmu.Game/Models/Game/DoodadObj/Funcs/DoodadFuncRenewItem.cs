using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.DoodadObj.Funcs
{
    public class DoodadFuncRenewItem : DoodadFuncTemplate
    {
        public uint SkillId { get; set; }
        
        public override void Use(Unit caster, Doodad owner, uint skillId)
        {
            _log.Debug("DoodadFuncRenewItem: SkillId {0}", SkillId);
            var func = DoodadManager.Instance.GetFunc(owner.FuncGroupId, SkillId);
            func?.Use(caster, owner, SkillId);
        }
    }
}
