using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Tasks.UnitMove
{
    public class CharMove : Task
    {
        private readonly PatrolCharacter _patrol;
        private readonly Character _ch;

        /// <summary>
        /// 初始化任务
        /// Initialization task
        /// </summary>
        /// <param name="patrol"></param>
        /// <param name="npc"></param>
        public CharMove(PatrolCharacter patrol, Character ch)
        {
            _patrol = patrol;
            _ch = ch;
        }

        /// <summary>
        /// 执行任务
        /// Perform tasks
        /// </summary>
        public override void Execute()
        {
            if (_ch.Hp > 0)
            {
                _patrol?.Apply(_ch);
            }
        }
    }
}
