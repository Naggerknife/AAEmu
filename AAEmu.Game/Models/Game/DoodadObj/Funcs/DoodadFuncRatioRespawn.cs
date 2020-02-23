using System;
using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Utils;

namespace AAEmu.Game.Models.Game.DoodadObj.Funcs
{
    public class DoodadFuncRatioRespawn : DoodadFuncTemplate
    {
        public int Ratio { get; set; }
        public uint SpawnDoodadId { get; set; }

        public override void Use(Unit caster, Doodad owner, uint skillId)
        {
            _log.Debug("DoodadFuncRatioRespawn : skillId {0}, Ratio {1}, SpawnDoodadId {2}", skillId, Ratio, SpawnDoodadId);

            if (caster is Character character)
            {
                var (newX, newY) = MathUtil.AddDistanceToFront(1, character.Position.X, character.Position.Y, character.Position.RotationZ); //TODO расстояние вперед 1 м

                // Doodad spawn
                //var doodad = new DoodadSpawner();
                //doodad.Id = 0;
                //doodad.UnitId = SpawnDoodadId; // mouse Id = 1087
                //doodad.Position = caster.Position.Clone();
                //doodad.Position.X = newX;
                //doodad.Position.X = newY;
                //doodad.Position.X = caster.Position.Z;
                //doodad.RespawnTime = Ratio;
                //doodad.Spawn(0);

                // NPC spawn
                var npcSpawner = new NpcSpawner
                {
                    Id = 0,
                    RespawnTime = 0,
                    DespawnTime = 1,  // minutes
                    UnitId = 7503,
                    Position = character.Position.Clone()
                };

                npcSpawner.Position.Y = newY + Rand.Next(-2, 2);
                npcSpawner.Position.Z = AppConfiguration.Instance.HeightMapsEnable
                    ? WorldManager.Instance.GetHeight(npcSpawner.Position.ZoneId, npcSpawner.Position.X, npcSpawner.Position.Y)
                    : npcSpawner.Position.Z;
                // looks in the direction of the character
                var angle = MathUtil.CalculateAngleFrom(npcSpawner.Position.X, npcSpawner.Position.Y, character.Position.X, character.Position.Y);
                var rotZ = MathUtil.ConvertDegreeToDirection(angle);
                npcSpawner.Position.RotationX = 0;
                npcSpawner.Position.RotationY = 0;
                npcSpawner.Position.RotationZ = rotZ;
                var npc = npcSpawner.Spawn(0);

                npc.Respawn = DateTime.MinValue; // not appear after death
                npc.Despawn = DateTime.Now.AddMinutes(1); // убираем через некоторое время

                npc.IsAutoAttack = true;

                var target = (BaseUnit)character;
                npc.CurrentTarget = target;
                npc.SetForceAttack(true);

                var combat = new Combat();
                combat.Execute(npc);
            }
        }
    }
}
