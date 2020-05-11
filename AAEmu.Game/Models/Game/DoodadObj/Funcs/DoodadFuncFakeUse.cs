using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.DoodadObj.Funcs
{
    public class DoodadFuncFakeUse : DoodadFuncTemplate
    {
        public uint SkillId { get; set; }
        public uint FakeSkillId { get; set; }
        public bool TargetParent { get; set; }

        public override void Use(Unit caster, Doodad owner, uint skillId)
        {
            _log.Debug("DoodadFuncFakeUse : skillId {0}, SkillId {1}, FakeSkillId {2}, TargetParent {3}",
                skillId, SkillId, FakeSkillId, TargetParent);

            if (SkillId == 0 || SkillId == null) { return; }

            var func = DoodadManager.Instance.GetFunc(owner.FuncGroupId, SkillId);
            if (func.NextPhase <= 0) { return; }
            owner.FuncGroupId = (uint)func.NextPhase;
            var nextfunc = DoodadManager.Instance.GetFunc(owner.FuncGroupId, 0);
            nextfunc?.Use(caster, owner, skillId);

            //var nextFunc = DoodadManager.Instance.GetFunc(owner.FuncGroupId, skillId);
            //if (nextFunc?.NextPhase == grp || nextFunc?.NextPhase == -1)
            //{
            //    return;
            //}
            //nextFunc?.Use(caster, owner, skillId);
        }
    }
}
