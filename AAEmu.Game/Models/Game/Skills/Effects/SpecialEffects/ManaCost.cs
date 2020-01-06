using System;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;
using NLog;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects
{
    public class ManaCost : ISpecialEffect
    {
        private static Logger _log = LogManager.GetCurrentClassLogger();
        private double _manaCost;
        public void Execute(Unit caster,
            SkillCaster casterObj,
            BaseUnit target,
            SkillCastTarget targetObj,
            CastAction castObj,
            Skill skill,
            SkillObject skillObject,
            DateTime time,
            int value1, int value2, int value3, int value4)
        {
            _log.Warn("value1 {0}, value2 {1}, value3 {2}, value4 {3}", value1, value2, value3, value4);

            if (!(caster is Character character))
            {
                return;
            }

            if(value1 != 0)
                _manaCost = character.Modifiers.ApplyModifiers(skill, SkillAttributeType.ManaCost, value1);
            else if (value2 != 0)
                _manaCost = character.Modifiers.ApplyModifiers(skill, SkillAttributeType.ManaCost, value2);

            character.ReduceCurrentMp(character, (int)_manaCost);
        }
    }
}
