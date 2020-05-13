using System;
using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.Units.Route;
using AAEmu.Game.Models.Tasks.UnitMove;

namespace AAEmu.Game.Models.Game.Skills.Effects
{
    public class DamageEffect : EffectTemplate
    {
        public DamageType DamageType { get; set; }
        public int FixedMin { get; set; }
        public int FixedMax { get; set; }
        public float Multiplier { get; set; }
        public bool UseMainhandWeapon { get; set; }
        public bool UseOffhandWeapon { get; set; }
        public bool UseRangedWeapon { get; set; }
        public int CriticalBonus { get; set; }
        public uint TargetBuffTagId { get; set; }
        public int TargetBuffBonus { get; set; }
        public bool UseFixedDamage { get; set; }
        public bool UseLevelDamage { get; set; }
        public float LevelMd { get; set; }
        public int LevelVaStart { get; set; }
        public int LevelVaEnd { get; set; }
        public float TargetBuffBonusMul { get; set; }
        public bool UseChargedBuff { get; set; }
        public uint ChargedBuffId { get; set; }
        public float ChargedMul { get; set; }
        public float AggroMultiplier { get; set; }
        public int HealthStealRatio { get; set; }
        public int ManaStealRatio { get; set; }
        public float DpsMultiplier { get; set; }
        public int WeaponSlotId { get; set; }
        public bool CheckCrime { get; set; }
        public uint HitAnimTimingId { get; set; }
        public bool UseTargetChargedBuff { get; set; }
        public uint TargetChargedBuffId { get; set; }
        public float TargetChargedMul { get; set; }
        public float DpsIncMultiplier { get; set; }
        public bool EngageCombat { get; set; }
        public bool Synergy { get; set; }
        public uint ActabilityGroupId { get; set; }
        public int ActabilityStep { get; set; }
        public float ActabilityMul { get; set; }
        public float ActabilityAdd { get; set; }
        public float ChargedLevelMul { get; set; }
        public bool AdjustDamageByHeight { get; set; }
        public bool UsePercentDamage { get; set; }
        public int PercentMin { get; set; }
        public int PercentMax { get; set; }
        public bool UseCurrentHealth { get; set; }
        public int TargetHealthMin { get; set; }
        public int TargetHealthMax { get; set; }
        public float TargetHealthMul { get; set; }
        public int TargetHealthAdd { get; set; }
        public bool FireProc { get; set; }

        public override bool OnActionTime => false;

        public override void Apply(Unit caster, SkillCaster casterObj, BaseUnit target, SkillCastTarget targetObj,
            CastAction castObj, Skill skill, SkillObject skillObject, DateTime time)
        {
            Log.Debug("DamageEffect" +
                      "DamageType {0}, FixedMin {1}, FixedMax {2}, Multiplier {3}, " +
                      "UseMainhandWeapon {4}, UseOffhandWeapon {5}, UseRangedWeapon {6}, " +
                      "CriticalBonus {7}, TargetBuffTagId {8}, TargetBuffBonus {9}, " +
                      "UseFixedDamage {10}, UseLevelDamage {11}, LevelMd {12}, " +
                      "LevelVaStart {13}, LevelVaEnd {14}, TargetBuffBonusMul {15}, " +
                      "UseChargedBuff {16}, ChargedBuffId {17}, ChargedMul {18}, " +
                      "AggroMultiplier {19}, HealthStealRatio {20}, ManaStealRatio {21}, " +
                      "DpsMultiplier {22}, WeaponSlotId {23}, CheckCrime {24}, " +
                      "HitAnimTimingId {25}, UseTargetChargedBuff {26}, TargetChargedBuffId {27}, " +
                      "TargetChargedMul {28}, DpsIncMultiplier {29}, EngageCombat {30}, " +
                      "Synergy {31}, ActabilityGroupId {32}, ActabilityStep {33}, " +
                      "ActabilityMul {34}, ActabilityAdd {35}, ChargedLevelMul {36}, " +
                      "AdjustDamageByHeight {37}, UsePercentDamage {38}, PercentMin {39}, " +
                      "PercentMax {40}, UseCurrentHealth {41}, TargetHealthMin {42}, " +
                      "TargetHealthMax {43}, TargetHealthMul {44}, TargetHealthAdd {45}, FireProc {46}",
                       DamageType, FixedMin, FixedMax, Multiplier, UseMainhandWeapon, UseOffhandWeapon,
                       UseRangedWeapon, CriticalBonus, TargetBuffTagId, TargetBuffBonus, UseFixedDamage,
                       UseLevelDamage, LevelMd, LevelVaStart, LevelVaEnd, TargetBuffBonusMul, UseChargedBuff,
                       ChargedBuffId, ChargedMul, AggroMultiplier, HealthStealRatio, ManaStealRatio,
                       DpsMultiplier, WeaponSlotId, CheckCrime, HitAnimTimingId, UseTargetChargedBuff,
                       TargetChargedBuffId, TargetChargedMul, DpsIncMultiplier, EngageCombat, Synergy,
                       ActabilityGroupId, ActabilityStep, ActabilityMul, ActabilityAdd, ChargedLevelMul,
                       AdjustDamageByHeight, UsePercentDamage, PercentMin, PercentMax, UseCurrentHealth,
                       TargetHealthMin, TargetHealthMax, TargetHealthMul, TargetHealthAdd, FireProc);

            if (!(target is Unit)) { return; }

            var trg = (Unit)target;
            var min = 0;
            var max = 0;

            if (UseFixedDamage)
            {
                min += FixedMin;
                max += FixedMax;
            }

            var unk = 0f;
            var unk2 = 1f;
            var skillLevel = 1;
            if (skill != null)
            {
                skillLevel = (skill.Level - 1) * skill.Template.LevelStep + skill.Template.AbilityLevel;
                if (skillLevel >= skill.Template.AbilityLevel)
                {
                    unk = 0.015f * (skillLevel - skill.Template.AbilityLevel + 1);
                }
                unk2 = (1 + unk) * 1.3f;
            }

            if (UseLevelDamage)
            {
                var levelMd = (unk + 1) * LevelMd;
                min += (int)(caster.LevelDps * levelMd + 0.5f);
                max += (int)((((skillLevel - 1) * 0.020408163f * (LevelVaEnd - LevelVaStart) + LevelVaStart) * 0.0099999998f + 1f) * caster.LevelDps * levelMd + 0.5f);
            }

            var dpsInc = 0f;

            switch (DamageType)
            {
                case DamageType.Melee:
                    Log.Debug("DamageEffect caster.DpsInc {0}", caster.DpsInc);
                    dpsInc = caster.DpsInc;
                    break;
                case DamageType.Magic:
                    Log.Debug("DamageEffect caster.MDps {0}, caster.MDpsInc {1}", caster.MDps, caster.MDpsInc);
                    dpsInc = caster.MDps + caster.MDpsInc;
                    break;
                case DamageType.Ranged:
                    Log.Debug("DamageEffect caster.RangedDpsInc {0}", caster.RangedDpsInc);
                    dpsInc = caster.RangedDpsInc;
                    break;
                    //case DamageType.Siege:
                    //    break;
                    //default:
                    //    throw new ArgumentOutOfRangeException();
            }

            var dps = 0f;
            if (UseMainhandWeapon)
            {
                Log.Debug("DamageEffect caster.Dps {0}", caster.Dps);
                dps += caster.Dps;
            }
            else if (UseOffhandWeapon)
            {
                Log.Debug("DamageEffect caster.OffhandDps {0}", caster.OffhandDps);
                dps += caster.OffhandDps;
            }
            else if (UseRangedWeapon)
            {
                Log.Debug("DamageEffect caster.RangedDps {0}", caster.RangedDps);
                dps += caster.RangedDps;
            }

            //// TODO vv--убрать этот костыль--vv
            //if (dps <= 0)
            //{
            //    dps = 15000f * caster.Level;
            //}
            //if (dpsInc <= 0)
            //{
            //    dpsInc = 2000f * caster.Level;
            //}
            //// TODO ^^--убрать этот костыль--^^

            min += (int)((DpsMultiplier * dps * 0.001f + DpsIncMultiplier * dpsInc * 0.001f) * unk2 + 0.5f);
            max += (int)((DpsMultiplier * dps * 0.001f + DpsIncMultiplier * dpsInc * 0.001f) * unk2 + 0.5f);
            min = (int)(min * Multiplier);
            max = (int)(max * Multiplier);
            var value = Rand.Next(min, max);

            caster.SummarizeDamage[0] += value;

            if (caster is Character chr1) // Character is in battle
            {
                chr1.IsInBattle = true;
            }
            if (target is Character chr2)
            {
                chr2.IsInBattle = true;
            }

            if (trg is Npc)
            {
                trg.BroadcastPacket(new SCAiAggroPacket(trg.ObjId, 1, caster.ObjId, caster.SummarizeDamage), true);
            }
            if (trg is Npc npc && npc.CurrentTarget != caster)
            {
                if (npc.Patrol == null || npc.Patrol.PauseAuto(npc))
                {
                    npc.CurrentTarget = caster;
                    npc.BroadcastPacket(new SCCombatEngagedPacket(caster.ObjId), true); // caster
                    npc.BroadcastPacket(new SCCombatEngagedPacket(npc.ObjId), true);    // target
                    npc.BroadcastPacket(new SCCombatFirstHitPacket(npc.ObjId, caster.ObjId, 0), true);
                    npc.BroadcastPacket(new SCAggroTargetChangedPacket(npc.ObjId, caster.ObjId), true);
                    npc.BroadcastPacket(new SCTargetChangedPacket(npc.ObjId, caster.ObjId), true);

                    TaskManager.Instance.Schedule(new UnitMove(new Track(), npc), TimeSpan.FromMilliseconds(100));
                }
            }

            trg.BroadcastPacket(new SCUnitDamagedPacket(castObj, casterObj, caster.ObjId, target.ObjId, value), true);
            trg.ReduceCurrentHp(caster, value);
        }
    }
}
