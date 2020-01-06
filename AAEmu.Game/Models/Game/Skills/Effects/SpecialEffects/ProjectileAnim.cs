using System;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Skills.Plots;
using AAEmu.Game.Models.Game.Units;
using NLog;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects
{
    public class ProjectileAnim : ISpecialEffect
    {
        private static Logger _log = LogManager.GetCurrentClassLogger();
        private ulong _itemId;
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

            //#region PlotEvent
            //var time2 = (ushort)(caster.Step.Flag != 0 ? caster.Step.Delay / 10 : 0);
            //var unkId = caster.Step.Casting || caster.Step.Channeling ? caster.ObjId : 0;
            //var casterPlotObj = new PlotObject(caster);
            //var targetPlotObj = new PlotObject(target);
            ////const byte targetUnitCount = 1;
            //caster.Step.Flag = 0;

            //if (casterObj is SkillItem item)
            //{
            //    _itemId = item.ItemId;
            //}

            //caster.BroadcastPacket(new SCPlotEventPacket(caster.TlIdPlot, caster.Step.Event.Id, caster.SkillId, casterPlotObj, targetPlotObj, unkId, time2, caster.Step.Flag, _itemId), true);
            //#endregion
        }
    }
}
