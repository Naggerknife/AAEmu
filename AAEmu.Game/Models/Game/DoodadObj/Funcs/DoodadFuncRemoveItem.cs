using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.DoodadObj.Funcs
{
    public class DoodadFuncRemoveItem : DoodadFuncTemplate
    {
        public uint ItemId { get; set; }
        public int Count { get; set; }

        public override void Use(Unit caster, Doodad owner, uint skillId)
        {
            _log.Debug("DoodadFuncRemoveItem: ItemId {0}, Count {1}", ItemId, Count);
            var character = (Character)caster;
            //var item = ItemManager.Instance.Create(ItemId, Count, 0);
            character?.Inventory.RemoveItem(ItemId, Count, Items.Actions.ItemTaskType.Destroy);
        }
    }
}
