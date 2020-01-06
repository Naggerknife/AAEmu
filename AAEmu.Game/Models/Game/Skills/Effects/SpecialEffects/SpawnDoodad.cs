using System;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Utils;
using NLog;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects
{
    public class SpawnDoodad : ISpecialEffect
    {
        private static Logger _log = LogManager.GetCurrentClassLogger();
        public void Execute(Unit caster,
            SkillCaster casterObj,
            BaseUnit target,
            SkillCastTarget targetObj,
            CastAction castObj,
            Skill skill,
            SkillObject skillObject,
            DateTime time,
            int value1, int value2, int value3, int value4)
        {
            // TODO ...
            _log.Warn("value1 {0}, value2 {1}, value3 {2}, value4 {3}", value1, value2, value3, value4);

            var owner = (Character)caster;
            var doodadSpawner = new DoodadSpawner
            {
                Id = owner.Id, // 0
                UnitId = (uint)value1, // doodad;
                Position = owner.Position.Clone()
            };
            var (newX2, newY2) = MathUtil.AddDistanceToFront(1, doodadSpawner.Position.X, doodadSpawner.Position.Y, doodadSpawner.Position.RotationZ); //TODO value3 throw distance L meters
            doodadSpawner.Position.X = newX2;
            doodadSpawner.Position.Y = newY2;

            doodadSpawner.Spawn(0);
        }
    }
}
