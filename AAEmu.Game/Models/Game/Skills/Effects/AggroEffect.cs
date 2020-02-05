using System;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects
{
    public class AggroEffect : EffectTemplate
    {
        public bool UseFixedAggro { get; set; }
        public int FixedMin { get; set; }
        public int FixedMax { get; set; }
        public bool UseLevelAggro { get; set; }
        public float LevelMd { get; set; }
        public int LevelVaStart { get; set; }
        public int LevelVaEnd { get; set; }
        public bool UseChargedBuff { get; set; }
        public uint ChargedBuffId { get; set; }
        public float ChargedMul { get; set; }

        public override bool OnActionTime => false;
        
        public override void Apply(Unit caster, SkillCaster casterObj, BaseUnit target, SkillCastTarget targetObj, CastAction castObj,
            Skill skill, SkillObject skillObject, DateTime time)
        {
            Log.Debug("AggroEffect: UseFixedAggro {0}, FixedMin {1}, FixedMax {2}, UseLevelAggro {3}, LevelMd {4}," +
                      " LevelVaStart {5}, LevelVaEnd {6}, UseChargedBuff {7}, ChargedBuffId {8}, ChargedMul {9}",
                UseFixedAggro, FixedMin, FixedMax, UseLevelAggro, LevelMd, LevelVaStart, LevelVaEnd, UseChargedBuff, ChargedBuffId, ChargedMul);
            if (!(caster is Npc npc))
            {
                return;
            }

            npc.IsAutoAttack = true;
            npc.CurrentTarget = target;
            npc.SetForceAttack(true);
            var combat = new Combat();
            combat.Execute(npc);
        }
    }
}
