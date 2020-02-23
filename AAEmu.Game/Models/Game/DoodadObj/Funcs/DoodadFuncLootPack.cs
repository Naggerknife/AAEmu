using System.Collections.Generic;
using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Utils;

namespace AAEmu.Game.Models.Game.DoodadObj.Funcs
{
    public class DoodadFuncLootPack : DoodadFuncTemplate
    {
        public uint LootPackId { get; set; }
        
        public override void Use(Unit caster, Doodad owner, uint skillId)
        {
            _log.Debug("DoodadFuncLootPack : skillId {0}, LootPackId {1}, ", skillId, LootPackId);

            var character = (Character)caster;
            if (character == null)
            {
                return;
            }

            var lootPacks = ItemManager.Instance.GetLootPacks(LootPackId);
            var dropRateMax = 0u;
            //var items = new List<Item>();
            var groupNum = 0;
            while (groupNum < 100)
            {
                groupNum += 1;
                _log.Warn("DoodadFuncLootPack : skillId {0}, LootPackId {1}, Group Num {2}", skillId, LootPackId, groupNum);

                var groupFound = false;
                foreach (var lp in lootPacks)
                {
                    if (lp.Group != groupNum)
                    {
                        continue;
                    }

                    dropRateMax += lp.DropRate;
                    groupFound = true;
                }
                var dropRateItem = Rand.Next(0, dropRateMax);
                var dropRateItemId = 0u;
                foreach (var lp in lootPacks)
                {
                    if ((lp.DropRate + dropRateItemId >= dropRateItem) && (lp.Group == groupNum))
                    {
                        var count = Rand.Next(lp.MinAmount, lp.MaxAmount);
                        var item = ItemManager.Instance.Create(lp.ItemId, count, lp.GradeId);
                        InventoryHelper.AddItemAndUpdateClient(character, item);
                        break;
                    }
                    else
                    {
                        dropRateItemId += lp.DropRate;
                    }
                }
                if (groupFound == false)
                    break;
            }
        }
    }
}
