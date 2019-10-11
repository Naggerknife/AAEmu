using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Error;
using AAEmu.Game.Models.Game.Skills;

namespace AAEmu.Game.Core.Packets.C2G
{
    public class CSStartSkillPacket : GamePacket
    {
        public CSStartSkillPacket() : base(0x052, 1)
        {
        }

        public override void Read(PacketStream stream)
        {
            Skill skill;
            var skillId = stream.ReadUInt32();

            var skillCasterType = stream.ReadByte(); // who applies
            var skillCaster = SkillCaster.GetByType((SkillCasterType)skillCasterType);
            skillCaster.Read(stream);

            var skillCastTargetType = stream.ReadByte(); // on whom apply
            var skillCastTarget = SkillCastTarget.GetByType((SkillCastTargetType)skillCastTargetType);
            skillCastTarget.Read(stream);

            var flag = stream.ReadByte();
            var flagType = flag & 15;
            var skillObject = SkillObject.GetByType((SkillObjectType)flagType);
            if (flagType > 0) skillObject.Read(stream);

            _log.Debug("StartSkill: Id {0}, flag {1}", skillId, flag);

            if (skillCaster is SkillItem)
            {
                var item = Connection.ActiveChar.Inventory.GetItem(((SkillItem)skillCaster).ItemId);
                if (item == null || skillId != item.Template.UseSkillId)
                {
                    Connection.ActiveChar.SendErrorMessage(ErrorMessageType.FailedToUseItem);
                    return;
                }
                Connection.ActiveChar.Quests.OnItemUse(item);
                skill = new Skill(SkillManager.Instance.GetSkillTemplate(skillId));
            }
            else if (SkillManager.Instance.IsDefaultSkill(skillId) || SkillManager.Instance.IsCommonSkill(skillId))
            {
                skill = new Skill(SkillManager.Instance.GetSkillTemplate(skillId));
            }
            else if (Connection.ActiveChar.Skills.Skills.ContainsKey(skillId))
            {
                skill = Connection.ActiveChar.Skills.Skills[skillId];
            }
            else
            {
                _log.Warn("StartSkill: Id {0}, undefined use type", skillId);
                Connection.ActiveChar.SendErrorMessage(ErrorMessageType.UnknownSkill);
                return;
            }

            skill.Start(Connection.ActiveChar, skillCaster, skillCastTarget, skillObject);
        }
    }
}
