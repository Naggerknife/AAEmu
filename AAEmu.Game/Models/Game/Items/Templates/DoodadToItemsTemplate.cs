using System.Collections.Generic;

namespace AAEmu.Game.Models.Game.Items.Templates
{
    public class DoodadToItemsTemplate
    {
        public uint Id { get; set; }
        public List<uint> ItemsId { get; set; }

        public DoodadToItemsTemplate()
        {
            ItemsId = new List<uint>();
        }
        public DoodadToItemsTemplate(List<uint> itemsId)
        {
            ItemsId = itemsId;
        }
    }
}
