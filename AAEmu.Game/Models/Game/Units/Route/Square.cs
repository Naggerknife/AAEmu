using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Units.Movements;
using AAEmu.Game.Utils;

namespace AAEmu.Game.Models.Game.Units.Route
{
    /// <summary>
    /// 正圆形巡航路线
    /// Round cruise route
    /// 根据圆点进行正圆形路线行走，适合平面地区
    /// According to the circular point, the regular circular route is suitable for the plane area.
    /// 非平整地区会造成NPC的遁地或飞空
    /// Non-uniform areas can cause NPC refuge or flight
    /// </summary>
    public class Square : Patrol
    {
        public short VelZ { get; set; } = 0;
        public sbyte Radius { get; set; } = 5;
        public short Degree { get; set; } = 360;

        /// <summary>
        /// 正方形巡航 / Square Cruise
        /// </summary>
        /// <param name="caster">触发角色 / Trigger role</param>
        /// <param name="npc">NPC</param>
        /// <param name="degree">角度 默认360度 / Default angle 360 degrees</param>
        public override void Execute(Npc npc)
        {
            var x = npc.Position.X;
            var y = npc.Position.Y;

            if (Count < Degree / 2)
            {
                npc.Position.X += (float)0.1;
            }
            else if (Count < Degree)
            {
                npc.Position.X -= (float)0.1;
            }

            if (Count < Degree / 4 || (Count > (Degree / 4 + Degree / 2) && Count < Degree))
            {
                npc.Position.Y += (float)0.1;
            }
            else if (Count < (Degree / 4 + Degree / 2))
            {
                npc.Position.Y -= (float)0.1;
            }

            // 模拟unit
            // Simulated unit
            const MoveTypeEnum type = (MoveTypeEnum)1;
            // 返回moveType对象
            // Return moveType object
            var moveType = (UnitMoveType)MoveType.GetType(type);

            // 改变NPC坐标
            // Change NPC coordinates
            moveType.X = npc.Position.X;
            moveType.Y = npc.Position.Y;
            moveType.Z = AppConfiguration.Instance.HeightMapsEnable ? WorldManager.Instance.GetHeight(npc.Position.ZoneId, npc.Position.X, npc.Position.Y) : npc.Position.Z;

            var angle = MathUtil.CalculateAngleFrom(x, y, npc.Position.X, npc.Position.Y);
            var rotZ = MathUtil.ConvertDegreeToDirection(angle);
            moveType.RotationX = 0;
            moveType.RotationY = 0;
            moveType.RotationZ = rotZ;

            moveType.Flags = 5;
            moveType.VelZ = VelZ;
            moveType.DeltaMovement = new sbyte[3];
            moveType.DeltaMovement[0] = 0;
            moveType.DeltaMovement[1] = 127;
            moveType.DeltaMovement[2] = 0;
            moveType.Stance = 1;    // COMBAT = 0x0, IDLE = 0x1
            moveType.Alertness = 0; // IDLE = 0x0, ALERT = 0x1, COMBAT = 0x2
            moveType.Time = (uint)Rand.Next(0, 10000); //Seq;    // должно всё время увеличиваться, для нормального движения

            npc.BroadcastPacket(new SCOneUnitMovementPacket(npc.ObjId, moveType), true);

            if (Count < Degree)
            {
                Repeat(npc);
            }
            else
            {
                moveType.DeltaMovement[1] = 0;
                npc.BroadcastPacket(new SCOneUnitMovementPacket(npc.ObjId, moveType), true);
                LoopAuto(npc);
            }
        }
    }
}
