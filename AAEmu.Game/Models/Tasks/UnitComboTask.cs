using System;
using System.Linq;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Tasks
{
    public class UnitComboTask : Task
    {
        private Unit _unit;

        public UnitComboTask(Unit unit)
        {
            _unit = unit;
        }

        public override void Execute()
        {
            if (_unit.ComboUnits.Count == 0 ||
                !_unit.ComboUnits.Values.Any(x => (DateTime.Now - x.lastHit).Seconds <= 15))
                _unit.StopCombo();
        }
    }
}
