using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G
{
    public class CSStopCastingPacket : GamePacket
    {
        public CSStopCastingPacket() : base(0x054, 1)
        {
        }

        public override async void Read(PacketStream stream)
        {
            var sid = stream.ReadUInt16(); // sid tl
            var pid = stream.ReadUInt16(); // pid tl
            var objId = stream.ReadBc();

            //if (Connection.ActiveChar.ObjId != objId
            //    || Connection.ActiveChar.SkillTask == null
            //    //|| Connection.ActiveChar.TlId != sid
            //    || Connection.ActiveChar.TlId != pid
            //    )
            //{
            //    return;
            //}
            //await Connection.ActiveChar.SkillTask.Cancel();
            //Connection.ActiveChar.SkillTask.Skill.Stop(Connection.ActiveChar);


            if (Connection.ActiveChar.ObjId == objId && Connection.ActiveChar.SkillTask != null && Connection.ActiveChar.SkillTask.Skill.TlId == pid)
            {
                await Connection.ActiveChar.SkillTask.Cancel();
                Connection.ActiveChar.SkillTask.Skill.Stop(Connection.ActiveChar);  // TODO mb sid
            }
            if (Connection.ActiveChar.ObjId == objId && Connection.ActiveChar.AutoAttackTask != null && Connection.ActiveChar.AutoAttackTask.Skill.TlId == pid)
            {
                await Connection.ActiveChar.AutoAttackTask.Cancel();
                Connection.ActiveChar.AutoAttackTask.Skill.Stop(Connection.ActiveChar);
            }

        }
    }
}
