using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.DoodadObj.Funcs
{
    public class DoodadFuncZoneReact : DoodadFuncTemplate
    {
        public uint ZoneGroupId { get; set; }
        public uint NextPhase { get; set; }
        
        public override void Use(Unit caster, Doodad owner, uint skillId)
        {
            _log.Debug("DoodadFuncZoneReact: skillId {0}, ZoneGroupId {1}, NextPhase {2}", skillId, ZoneGroupId, NextPhase);

            // perform action
            owner.BroadcastPacket(new SCDoodadPhaseChangedPacket(owner), true);
        }
    }
}
