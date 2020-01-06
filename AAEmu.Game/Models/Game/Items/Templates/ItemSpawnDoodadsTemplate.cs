using System;
using System.Collections.Generic;

namespace AAEmu.Game.Models.Game.Items.Templates
{
    public class ItemSpawnDoodadsTemplate : ItemTemplate
    {
        public override Type ClassType => typeof(Item);
        public List<uint> ItemId { get; set; }
        public uint DoodadId { get; set; }
    }
}
