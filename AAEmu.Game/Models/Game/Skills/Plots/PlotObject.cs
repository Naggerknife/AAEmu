using AAEmu.Commons.Network;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.World;

namespace AAEmu.Game.Models.Game.Skills.Plots
{
    public class PlotObject : PacketMarshaler
    {
        public PlotObjectType Type { get; set; }
        public uint UnitId { get; set; }
        public Point Position { get; set; }

        public PlotObject(BaseUnit unit)
        {
            Type = PlotObjectType.Unit;
            UnitId = unit.ObjId;
        }

        public PlotObject(uint unitId)
        {
            Type = PlotObjectType.Unit;
            UnitId = unitId;
        }

        public PlotObject(Point position)
        {
            Type = PlotObjectType.Position;
            Position = position;
        }

        public override PacketStream Write(PacketStream stream)
        {
            stream.Write((byte)Type);

            switch (Type)
            {
                case PlotObjectType.Unit:
                    stream.WriteBc(UnitId);
                    break;
                case PlotObjectType.Position:
                    stream.WritePosition(Position.X, Position.Y, Position.Z);
                    stream.Write(Position.RotationX);
                    stream.Write(Position.RotationY);
                    stream.Write(Position.RotationZ);
                    break;
            }

            return stream;
        }
    }
}
