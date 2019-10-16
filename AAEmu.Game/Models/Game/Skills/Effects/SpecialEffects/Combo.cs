using System;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Tasks.Skills;
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
            int value1,
            int value2,
            int value3,
            int value4)
        {
            //TODO: this should not auto cast the skill, just make it so that the skill on the hotbar changes to the next skill temporarily (for value2 amount of time)
            _log.Warn("Special effects: Combo");
            if (value1 > 0 && caster != null && target != null)
            {
                var comboSkill = new Skill(SkillManager.Instance.GetSkillTemplate((uint)value1));

                TaskManager.Instance.Schedule(new Tasks.Skills.SkillUse(comboSkill,
                        caster,
                        casterObj,
                        targetObj,
                        skillObject),
                    TimeSpan.FromMilliseconds(value2));
            }
        }
    }
}
