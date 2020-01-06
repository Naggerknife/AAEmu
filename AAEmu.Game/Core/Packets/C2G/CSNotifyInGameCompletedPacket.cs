using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G
{
    public class CSNotifyInGameCompletedPacket : GamePacket
    {
        public CSNotifyInGameCompletedPacket() : base(0x02a, 1)
        {
        }

        public override void Read(PacketStream stream)
        {
            if (Connection.ActiveChar.Family > 0)
            {
                FamilyManager.Instance.OnCharacterLogin(Connection.ActiveChar);
            }

            Connection.ActiveChar.Inventory.UpdateEquipmentBuffs(); //Rebuild equipment buffs
            Connection.ActiveChar.StartRegen();
            _log.Info("NotifyInGameCompleted");
        }
    }
}
