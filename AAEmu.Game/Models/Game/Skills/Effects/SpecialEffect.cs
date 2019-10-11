using System;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects
{
    public interface ISpecialEffect
    {
        void Execute(Unit caster,
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
            int value4);
    }
    public class SpecialEffect : EffectTemplate
    {
        public SpecialEffectType SpecialEffectTypeId { get; set; } // TODO inspect
        public int Value1 { get; set; }
        public int Value2 { get; set; }
        public int Value3 { get; set; }
        public int Value4 { get; set; }

        public override bool OnActionTime => false;

        public override void Apply(Unit caster,
            SkillCaster casterObj,
            BaseUnit target,
            SkillCastTarget targetObj,
            CastAction castObj,
            Skill skill,
            SkillObject skillObject,
            DateTime time)
        {
            Log.Debug(
                "SpecialEffect, Special: {0}, Value1: {1}, Value2: {2}, Value3: {3}, Value4: {4}",
                SpecialEffectTypeId, Value1, Value2, Value3, Value4
                );

            var classType = Type.GetType("AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects." + SpecialEffectTypeId);
            if (classType == null)
            {
                Log.Error("Unknown special effect: {0}", SpecialEffectTypeId);
                return;
            }

            var action = (ISpecialEffect)Activator.CreateInstance(classType);
            action.Execute(caster, casterObj, target, targetObj, castObj, skill, skillObject, time, Value1, Value2, Value3, Value4);
        }
    }
}
