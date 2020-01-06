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

            if (Connection.ActiveChar.ObjId != objId || Connection.ActiveChar.SkillTask == null || Connection.ActiveChar.TlId != sid || Connection.ActiveChar.TlIdPlot != pid)
            {
                return;
            }
            await Connection.ActiveChar.SkillTask.Cancel();
            Connection.ActiveChar.SkillTask.Skill.Stop(Connection.ActiveChar);
        }
    }
}
