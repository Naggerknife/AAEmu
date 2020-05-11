using AAEmu.Commons.Utils;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Funcs;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.World.Interactions
{
    public class Looting : IWorldInteraction
    {
        public void Execute(Unit caster, SkillCaster casterType, BaseUnit target, SkillCastTarget targetType, 
            uint skillId, uint doodadId, DoodadFuncTemplate objectFunc)
        {
            var character = (Character)caster;
            if (character == null) { return; }
            var chance = Rand.Next(0, 10000);
            if (!(objectFunc is DoodadFuncLootItem obj)) { return; }
            if (chance > obj.Percent) { return; }
            var count = Rand.Next(obj.CountMin, obj.CountMax);
            //TODO: Buffer
            character.Inventory.AddNewItem(doodadId, count, 0, ItemTaskType.Loot);
        }
    }
}
