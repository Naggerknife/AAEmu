using System;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.Units;

using NLog;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects
{
    public class Combo : ISpecialEffect
    {
        private static Logger _log = LogManager.GetCurrentClassLogger();
        public void Execute(Unit caster,
            SkillCaster casterObj,
            BaseUnit target,
            SkillCastTarget targetObj,
            CastAction castObj,
            Skill skill,
            SkillObject skillObject,
            DateTime time,
            int comboSkillId, int timeFromNow, int value3, int value4)
        {
            _log.Warn("comboSkillId {0}, timeFromNow {1}, value3 {2}, value4 {3}", comboSkillId, timeFromNow, value3, value4);

            if (comboSkillId <= 0 || caster == null || target == null)
            {
                return;
            }
            // TODO: this should not auto cast the skill, just make it so that the skill on the hotbar changes to the next skill temporarily (for value2 amount of time)
            var comboSkill = new Skill(SkillManager.Instance.GetSkillTemplate((uint)comboSkillId));
            TaskManager.Instance.Schedule(new Tasks.Skills.SkillUse(comboSkill, caster, casterObj, targetObj, skillObject), TimeSpan.FromMilliseconds(timeFromNow));
        }
    }
}
