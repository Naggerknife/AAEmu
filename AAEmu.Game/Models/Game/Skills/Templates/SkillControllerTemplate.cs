using System;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Templates
{
    public class SkillControllerTemplate : EffectTemplate
    {
        public uint KindId { get; set; }
        public uint StartAnimId { get; set; }
        public uint EndAnimId { get; set; }
        public uint TransitionAnim1Id { get; set; }
        public uint TransitionAnim2Id { get; set; }
        public string StrValue1 { get; set; }
        public int[] Value { get; set; }
        public byte ActiveWeaponId { get; set; }
        public uint EndSkillId { get; set; }
        public override bool OnActionTime { get; }

        public SkillControllerTemplate()
        {
            Value = new int[15];
        }

        public override void Apply(Unit caster, SkillCaster casterObj, BaseUnit target, SkillCastTarget targetObj, CastAction castObj,
            Skill skill, SkillObject skillObject, DateTime time)
        {
            Log.Debug("SkillControllerTemplate");
        }
    }
}
