using System;
using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Tasks.Doodads;

namespace AAEmu.Game.Models.Game.DoodadObj.Funcs
{
    public class DoodadFuncFinal : DoodadFuncTemplate
    {
        public int After { get; set; }
        public bool Respawn { get; set; }
        public int MinTime { get; set; }
        public int MaxTime { get; set; }
        public bool ShowTip { get; set; }
        public bool ShowEndTime { get; set; }
        public string Tip { get; set; }

        public override void Use(Unit caster, Doodad owner, uint skillId)
        {
            _log.Debug("DoodadFuncFinal: After {0}, MinTime {1}, MaxTime {2}, Tip {3}, Respawn {4}", After, MinTime, MaxTime, Tip, Respawn);

            var delay = Rand.Next(MinTime, MaxTime);

            owner.GrowthTime = DateTime.Now.AddMilliseconds(delay); // TODO ... need here?
            owner.FuncTask = new DoodadFuncFinalTask(caster, owner, skillId, Respawn);
            TaskManager.Instance.Schedule(owner.FuncTask, TimeSpan.FromMilliseconds(After)); // After ms remove the object from visibility
        }
    }
}
