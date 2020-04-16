using System;

using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.World;
using AAEmu.Game.Models.Tasks.UnitMove;

namespace AAEmu.Game.Models.Game.Units
{
    /// <summary>
    /// Unit 巡逻类
    /// Unit Patrol Class
    /// </summary>
    public abstract class PatrolCharacter
    {
        /// <summary>
        /// 是否正在执行巡逻任务
        /// Are patrols under way?
        /// 默认为 False
        /// The default is False
        /// </summary>
        public bool Running { get; set; } = true;

        /// <summary>
        /// 是否为循环
        /// Is it a cycle?
        /// 默认为 True
        /// Default is True
        /// </summary>
        public bool Loop { get; set; } = true;

        /// <summary>
        /// 循环间隔 毫秒
        /// Cyclic interval milliseconds
        /// </summary>
        public double LoopDelay { get; set; }

        /// <summary>
        /// 执行进度 0-100
        /// Progress of implementation 0-100
        /// </summary>
        public sbyte Step { get; set; }

        /// <summary>
        /// 中断 True
        /// Interrupt True
        /// 如被攻击或其他行为改变自身状态等 是否终止路线
        /// Whether or not to terminate a route, such as being attacked or changing one's own state by other acts
        /// </summary>
        public bool Interrupt { get; set; } = true;

        /// <summary>
        /// 执行顺序编号
        /// Execution Sequence Number
        /// 每次执行必须递增序号，否则重复序号的动作不被执行
        /// Each execution must increment the serial number, otherwise the action of repeating the serial number will not be executed.
        /// </summary>
        public uint Seq { get; set; } = 0;

        /// <summary>
        /// 当前执行次数
        /// Current execution times
        /// </summary>
        protected uint Count { get; set; } = 0;

        /// <summary>
        /// 暂停巡航点
        /// Suspension of cruise points
        /// </summary>
        protected Point PausePosition { get; set; }

        /// <summary>
        /// 上次任务
        /// Last mission
        /// </summary>
        public PatrolCharacter LastPatrol { get; set; }

        /// <summary>
        /// 放弃任务 / Abandon mission
        /// </summary>
        public bool Abandon { get; set; } = false;

        /// <summary>
        /// 执行巡逻任务
        /// Perform patrol missions
        /// </summary>
        /// <param name="ch"></param>
        public void Apply(Character ch)
        {
            //如果NPC不存在或不处于巡航模式或者当前执行次数不为0
            //If NPC does not exist or is not in cruise mode or the current number of executions is not zero
            //if (ch.Patrol == null || ch.Patrol.Running == false && this != ch.Patrol || ch.Patrol.Running && this == ch.Patrol)
            {
                //如果上次巡航模式处于暂停状态则保存上次巡航模式
                //If the last cruise mode is suspended, save the last cruise mode
                if (ch.Patrol != null && ch.Patrol != this && !ch.Patrol.Abandon)
                {
                    LastPatrol = ch.Patrol;
                }
                //++Count;
                Count += 2;
                ++Seq; //Seq = (uint)Rand.Next(0, 10000);
                Running = true;
                ch.Patrol = this;
                Execute(ch);
            }
        }

        public abstract void Execute(Character ch);

        /// <summary>
        /// 再次执行任务
        /// Perform the task again
        /// </summary>
        /// <param name="ch"></param>
        /// <param name="time"></param>
        /// <param name="patrol"></param>
        public void Repeat(Character ch, double time = 100, PatrolCharacter patrol = null)
        {
            if (!(patrol ?? this).Abandon)
            {
                TaskManager.Instance.Schedule(new CharMove(patrol ?? this, ch), TimeSpan.FromMilliseconds(time));
            }
        }

        public bool PauseAuto(Character ch)
        {
            if (!Interrupt && ch.Patrol.Running)
            {
                return false;
            }

            Pause(ch);
            return true;
        }

        public void Pause(Character ch)
        {
            Running = false;
            PausePosition = ch.Position.Clone();
        }

        public void Stop(Character ch)
        {
            Running = false;
            Abandon = true;

            Recovery(ch);
        }

        public void Recovery(Character ch)
        {
            // 如果当前巡航处于暂停状态则恢复当前巡航
            // Resume current cruise if current cruise is paused
            if (!Abandon && Running == false)
            {
                ch.Patrol.Running = true;
                Repeat(ch);
                return;
            }
            // 如果上次巡航不为null
            // If the last cruise is not null
            const float tolerance = 0.1f;
            if (LastPatrol == null || Running)
            {
                return;
            }

            if (
                Math.Abs(ch.Position.X - LastPatrol.PausePosition.X) < tolerance
                &&
                Math.Abs(ch.Position.Y - LastPatrol.PausePosition.Y) < tolerance
                &&
                Math.Abs(ch.Position.Z - LastPatrol.PausePosition.Z) < tolerance
            )
            {
                LastPatrol.Running = true;
                ch.Patrol = LastPatrol;
                // 恢复上次巡航
                // Resume last cruise
                Repeat(ch, 500, LastPatrol);
            }
            else
            {
                // 创建直线巡航回归上次巡航暂停点
                // Create a straight cruise to return to the last cruise pause
                Stop(ch);
            }
        }

        public void LoopAuto(Character ch)
        {
            if (Loop)
            {
                Count = 0;
                Seq = (uint)Rand.Next(0, 10000); // = 0
                Repeat(ch, LoopDelay);
            }
            else
            {
                // 非循环任务则终止本任务并尝试恢复上次任务
                // Acyclic tasks terminate this task and attempt to resume the last task
                Stop(ch);
            }
        }
    }
}
