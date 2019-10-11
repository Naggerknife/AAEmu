using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.DoodadObj.Funcs
{
    public class DoodadFuncLootItem : DoodadFuncTemplate
    {
        public uint WorldInteractionId { get; set; }
        public uint ItemId { get; set; }
        public int CountMin { get; set; }
        public int CountMax { get; set; }
        public int Percent { get; set; }
        public int RemainTime { get; set; }
        public uint GroupId { get; set; }

        public override void Use(Unit caster, Doodad owner, uint skillId)
        {
            _log.Debug("DoodadFuncLootItem: WorldInteractionId {0}, ItemId {1}, CountMin {2}, CountMax {3}, Percent {4}, RemainTime {5}, GroupId {6}", WorldInteractionId, ItemId, CountMin, CountMax, Percent, RemainTime, GroupId);
            var character = (Character)caster;
            if (character == null) return;

            var chance = Rand.Next(0, 10000);
            if (chance > Percent) return;

            var count = Rand.Next(CountMin, CountMax);

            //TODO: Buffer
            var item = ItemManager.Instance.Create(ItemId, count, 0);
            character.Inventory.AddExistingItem(item, true, SlotType.Inventory, Items.Actions.ItemTaskType.Loot);
        }
    }
}
