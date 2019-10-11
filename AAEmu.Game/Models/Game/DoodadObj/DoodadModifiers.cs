namespace AAEmu.Game.Models.Game.DoodadObj
{
    public class DoodadModifiers
    {
		public uint Id { get; set; }
        public int OwnerId { get; set; }
        public string OwnerType { get; set; }
		public int DoodadId { get; set; }
		public int TagId { get; set; }
		public int DoodadAttributeId { get; set; }
		public int UnitModifierTypeId { get; set; }
		public uint value { get; set; }
    }
}