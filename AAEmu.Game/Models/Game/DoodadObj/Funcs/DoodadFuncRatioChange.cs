using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.DoodadObj.Funcs
{
    public class DoodadFuncRatioChange : DoodadFuncTemplate
    {
        public int Ratio { get; set; }
        public uint NextPhase { get; set; }

        public override void Use(Unit caster, Doodad owner, uint skillId)
        {
            _log.Debug("DoodadFuncRatioChange : Ratio {0}, NextPhase {1}, SkillId {2}", Ratio, NextPhase, skillId);

            var character = (Character)caster;
            if (character == null) return;

            var chance = Rand.Next(0, 10000);
            if (chance > Ratio) return;

            var count = 1;
            var itemId = 8022u;  // TODO что-то не так, откуда ему взяться?

            var item = ItemManager.Instance.Create(itemId, count, 0);
            character.Inventory.AddExistingItem(item, true, SlotType.Inventory, Items.Actions.ItemTaskType.Loot);

        }
    }
}
