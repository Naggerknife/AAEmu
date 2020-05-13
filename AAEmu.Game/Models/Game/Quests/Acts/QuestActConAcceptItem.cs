using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Quests.Templates;

namespace AAEmu.Game.Models.Game.Quests.Acts
{
    public class QuestActConAcceptItem : QuestActTemplate
    {
        public uint ItemId { get; set; }
        public bool Cleanup { get; set; }
        public bool DropWhenDestroy { get; set; }
        public bool DestroyWhenDrop { get; set; }

        public override bool Use(Character character, Quest quest, int objective) // triggered by using things
        {
            _log.Debug("QuestActConAcceptItem: ItemId {0}, Cleanup {1}, DropWhenDestroy {2}, DestroyWhenDrop {3}",
                ItemId, Cleanup, DropWhenDestroy, DestroyWhenDrop);
            
            // TODO Cleanup
            return character.Inventory.CheckItems(ItemId, 1);
        }
    }
}
