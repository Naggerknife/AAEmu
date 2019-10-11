using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Team;

namespace AAEmu.Game.Core.Packets.G2C
{
    public class SCTeamMemberRoleChangedPacket : GamePacket
    {
        private readonly uint _teamId;
        private readonly uint _memberId;
        private readonly MemberRoleType _role;

        public SCTeamMemberRoleChangedPacket(uint teamId, uint memberId, MemberRoleType role) : base(SCOffsets.SCTeamMemberRoleChangedPacket, 1)
        {
            _teamId = teamId;
            _memberId = memberId;
            _role = role;
        }

        public override PacketStream Write(PacketStream stream)
        {
            stream.Write(_teamId);
            stream.Write(_memberId);
            stream.Write((byte)_role);
            return stream;
        }
    }
}
