using System;
using System.Collections.Concurrent;
using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.Game.Duels;
using AAEmu.Game.Models.Game.Error;
using AAEmu.Game.Models.Tasks.Doodads;
using AAEmu.Game.Models.Tasks.Duels;
using AAEmu.Game.Utils;
using NLog;

namespace AAEmu.Game.Core.Managers
{
    public class DuelManager : Singleton<DuelManager>
    {
        protected static Logger _log = LogManager.GetCurrentClassLogger();

        private DoodadSpawner _combatFlag;
        private const double Delay = 1000; // 1 sec
        private const float DistanceForSurrender = 75; // square 75 meters
        private const double DuelDurationTime = 5;    // 5 min

        // одновременно может быть несколько дуэлей
        private ConcurrentDictionary<uint, Duel> _duels;

        protected DuelManager()
        {
            _duels = new ConcurrentDictionary<uint, Duel>();
        }

        public bool Initialize()
        {
            _log.Info("Initialising Duel Manager...");
            return true;
        }

        private void DuelAdd(Duel duel)
        {
            if (!_duels.ContainsKey(duel.Challenger.Id))
                _duels.TryAdd(duel.Challenger.Id, duel);

            if (!_duels.ContainsKey(duel.Challenged.Id))
                _duels.TryAdd(duel.Challenged.Id, duel);
        }

        private void DuelRemove(Duel duel)
        {
            _duels.TryRemove(duel.Challenger.Id, out _);
            _duels.TryRemove(duel.Challenged.Id, out _);
        }
        public void DuelRequest(Character challenger, uint challengedId)
        {
            var challenged = WorldManager.Instance.GetCharacterById(challengedId);
            var duel = new Duel(challenger, challenged);
            DuelAdd(duel);

            // приходит ID того кого вызываем на дуэль, мы отправляет свое ID т.е. того кого вызвали на дуэль
            challenged.SendPacket(new SCDuelChallengedPacket(challenger.Id)); // we send only to the enemy
        }

        public void DuelAccepted(Character challenged, uint challengerId)
        {
            if (challenged == null)
            {
                throw new ArgumentNullException(nameof(challenged));
            }
            // приходит ID того кто вызывает на дуэль, мы отправляет свое ID т.е. того кого вызвали на дуэль
            try
            {
                var duel = _duels[challengerId];

                if (duel.DuelStarted == false)
                {
                    duel.DuelStarted = true;
                    // spawn flag
                    _combatFlag = new DoodadSpawner
                    {
                        Id = 0,
                        UnitId = 5014, // Combat Flag Id=5014;
                        Position = duel.Challenger.Position.Clone()
                    };
                    _combatFlag.Position.X = duel.Challenger.Position.X - (duel.Challenger.Position.X - duel.Challenged.Position.X) / 2;
                    _combatFlag.Position.Y = duel.Challenger.Position.Y - (duel.Challenger.Position.Y - duel.Challenged.Position.Y) / 2;
                    _combatFlag.Position.Z = AppConfiguration.Instance.HeightMapsEnable
                        ? WorldManager.Instance.GetHeight(_combatFlag.Position.ZoneId, _combatFlag.Position.X, _combatFlag.Position.Y)
                        : duel.Challenger.Position.Z - (duel.Challenger.Position.Z - duel.Challenged.Position.Z) / 2;

                    duel.DuelFlag = _combatFlag.Spawn(0); // set CombatFlag

                    // make the flag flutter in the wind
                    const uint NextPhase = 12830u;
                    const uint SkillId = 0u;
                    duel.DuelFlag.GrowthTime = DateTime.Now.AddMilliseconds(100);
                    duel.DuelFlag.FuncTask = new DoodadFuncGrowthTask(duel.Challenger, duel.DuelFlag, SkillId, NextPhase);
                    TaskManager.Instance.Schedule(duel.DuelFlag.FuncTask, TimeSpan.FromMilliseconds(10));

                    //Schedule duel start task.
                    duel.DuelStartTask = new DuelStartTask(duel.Challenger.Id);
                    TaskManager.Instance.Schedule(duel.DuelStartTask, TimeSpan.FromSeconds(3));
                }
                else
                    _log.Warn("DuelAccepted: Duel with challengerId = {0} is already started", challengerId);
            }
            catch (Exception e)
            {
                // id отсутствует в базе
                _log.Warn("DuelAccepted: Id = {0} not found in duels[], error code: {1}", challengerId, e);
            }
        }

        public void DuelStart(uint id)
        {
            try
            {
                var duel = _duels[id];
                duel.SendPacketsBoth(new SCDuelStartedPacket(duel.Challenger.ObjId, duel.Challenged.ObjId));
                duel.SendPacketChallenged(new SCAreaChatBubblePacket(true, duel.Challenger.ObjId, 543));
                duel.SendPacketChallenger(new SCAreaChatBubblePacket(true, duel.Challenged.ObjId, 543));
                duel.SendPacketsBoth(new SCDuelStartCountdownPacket());
                duel.SendPacketChallenged(new SCDuelStatePacket(duel.Challenger.ObjId, duel.DuelFlag.ObjId));
                duel.SendPacketChallenger(new SCDuelStatePacket(duel.Challenged.ObjId, duel.DuelFlag.ObjId));
                // Player can be attacked
                duel.SendPacketsBoth(new SCCombatEngagedPacket(duel.Challenger.ObjId));
                duel.SendPacketsBoth(new SCCombatEngagedPacket(duel.Challenged.ObjId));
                // final operations after a duel
                duel.DuelEndTimerTask = new DuelEndTimerTask(duel, duel.Challenger.Id);
                TaskManager.Instance.Schedule(duel.DuelEndTimerTask, TimeSpan.FromMinutes(DuelDurationTime));
                // запустим проверку на дистанцию
                _ = DuelDistanceСheck(duel.Challenger.Id);
            }
            catch (Exception e)
            {
                //id отсутствует в базе
                _log.Warn("DuelStart: Id = {0} not found in duels[], error code: {1}", id, e);
            }
        }

        public void DuelCancel(uint challengerId, ErrorMessageType errorMessage)
        {
            try
            {
                var duel = _duels[challengerId];
                duel.DuelAllowed = false;
                if (errorMessage != 0)
                    duel.Challenger.SendErrorMessage((ErrorMessageType)errorMessage);

                _log.Warn("DuelCancel: Duel with challengerId = {0} canceled, error code: {1}", challengerId, errorMessage);
                DuelCleanUp(challengerId);
            }
            catch (Exception e)
            {
                //id отсутствует в базе
                _log.Warn("DuelCancel: Id = {0} not found in duels[], error code: {1}", challengerId, e);
            }
        }

        private void DuelCleanUp(uint id)
        {
            try
            {
                var duel = _duels[id];

                if (duel.DuelStartTask != null)
                {
                    _ = duel.DuelStartTask.Cancel();
                    duel.DuelStartTask = null;
                }

                if (duel.DuelEndTimerTask != null)
                {
                    _ = duel.DuelEndTimerTask.Cancel();
                    duel.DuelEndTimerTask = null;
                }

                DuelRemove(duel);
            }
            catch (Exception e)
            {
                // id отсутствует в базе
                _log.Warn("CleanUpDuel: Id = {0} not found in duels[], error code: {1}", id, e);
            }
        }

        public void DuelStop(uint id, DuelDetType det, uint loseId = 0)
        {
            try
            {
                var duel = _duels[id];

                duel.DuelAllowed = false;

                // Duel is over, det 00=lose, 01=win, 02=surrender (Fled beyond the flag action border), 03=draw
                if (det == DuelDetType.Draw)
                {
                    duel.SendPacketChallenged(new SCDuelEndedPacket(duel.Challenger.Id, duel.Challenged.Id, duel.Challenger.ObjId, duel.Challenged.ObjId, det));
                    duel.SendPacketChallenger(new SCDuelEndedPacket(duel.Challenged.Id, duel.Challenger.Id, duel.Challenged.ObjId, duel.Challenger.ObjId, det));
                }
                else if (loseId != 0)
                {
                    if (loseId == duel.Challenger.Id)
                        duel.SendPacketsBoth(new SCDuelEndedPacket(duel.Challenged.Id, duel.Challenger.Id, duel.Challenged.ObjId, duel.Challenger.ObjId, det));

                    else if (loseId == duel.Challenged.Id)
                        duel.SendPacketsBoth(new SCDuelEndedPacket(duel.Challenger.Id, duel.Challenged.Id, duel.Challenger.ObjId, duel.Challenged.ObjId, det));
                }

                // Duel Status - Duel ended
                duel.SendPacketsBoth(new SCDuelStatePacket(duel.Challenged.ObjId, 0));
                duel.SendPacketsBoth(new SCDuelStatePacket(duel.Challenger.ObjId, 0));

                if (duel.DuelFlag != null)
                {
                    duel.DuelFlag.Delete(); //Remove Flag
                    // Remove Flag
                    duel.SendPacketsBoth(new SCDoodadRemovedPacket(duel.DuelFlag.ObjId, 0));
                }


                // Player cannot be attacked
                duel.SendPacketsBoth(new SCCombatClearedPacket(duel.Challenger.ObjId));
                duel.SendPacketsBoth(new SCCombatClearedPacket(duel.Challenged.ObjId));

                DuelCleanUp(id);
                _log.Warn("DuelStop: Duel ended");
            }
            catch (Exception e)
            {
                // id отсутствует в базе
                _log.Warn("DuelStop: Id = {0} not found in duels[], error code: {1}", id, e);
            }
        }

        public DuelDistance DuelDistanceСheck(uint id)
        {
            try
            {
                var duel = _duels[id];
                // проверяем, сбежали от флага или нет
                var currentDistance = MathUtil.CalculateDistance(duel.DuelFlag.Position, duel.Challenger.Position, true);
                if (currentDistance >= DistanceForSurrender)
                {
                    // отключаем таймер
                    if (duel.DuelDistanceСheckTask == null)
                        return DuelDistance.ChallengerFar; // сдается тот, кто вызывал на дуэль, т.е. убежал от флага

                    _ = duel.DuelDistanceСheckTask.Cancel();
                    duel.DuelDistanceСheckTask = null;
                    return DuelDistance.ChallengerFar; // сдается тот, кто вызывал на дуэль, т.е. убежал от флага
                }
                // проверяем, сбежали от флага или нет
                currentDistance = MathUtil.CalculateDistance(duel.DuelFlag.Position, duel.Challenged.Position, true);
                if (currentDistance >= DistanceForSurrender)
                {
                    // отключаем таймер
                    if (duel.DuelDistanceСheckTask == null)
                        return DuelDistance.ChallengedFar; // сдается тот, кого вызвали на дуэль, т.е. убежал от флага

                    _ = duel.DuelDistanceСheckTask.Cancel();
                    duel.DuelDistanceСheckTask = null;
                    return DuelDistance.ChallengedFar; // сдается тот, кого вызвали на дуэль, т.е. убежал от флага
                }
                // изменил проверку дистанции с цикла, на вызов по таймеру
                duel.DuelDistanceСheckTask = new DuelDistanceСheckTask(duel);
                TaskManager.Instance.Schedule(duel.DuelDistanceСheckTask, TimeSpan.FromMilliseconds(Delay));
            }
            catch (Exception e)
            {
                //id отсутствует в базе
                _log.Warn("DistanceСheck: Id = {0} not found in duels[], error code: {1}", id, e);
                return DuelDistance.Error;  // рядом с флагом
            }
            return DuelDistance.Near;  // рядом с флагом
        }
    }
}
