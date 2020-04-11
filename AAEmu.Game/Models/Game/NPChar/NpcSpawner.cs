using System;
using System.Collections.Generic;
using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers.Id;
using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.Units.Route;
using AAEmu.Game.Models.Game.World;
using NLog;

namespace AAEmu.Game.Models.Game.NPChar
{
    public class NpcSpawner : Spawner<Npc>
    {
        private static Logger _log = LogManager.GetCurrentClassLogger();

        private List<Npc> _spawned;
        private Npc _lastSpawn;
        private int _scheduledCount;
        private int _spawnCount;

        public uint Count { get; set; }

        public NpcSpawner()
        {
            _spawned = new List<Npc>();
            Count = 1;
        }

        public List<Npc> SpawnAll()
        {
            var list = new List<Npc>();
            for (var num = _scheduledCount; num < Count; num++)
            {
                var npc = Spawn(0);
                if (npc != null)
                {
                    list.Add(npc);
                }
            }

            return list;
        }

        public override Npc Spawn(uint objId)
        {
            var npc = NpcManager.Instance.Create(objId, UnitId);
            if (npc == null)
            {
                _log.Warn("Npc {0}, from spawn not exist at db", UnitId);
                return null;
            }

            npc.Spawner = this;
            npc.Position = Position.Clone();
            if (npc.Position == null)
            {
                _log.Error("Can't spawn npc {1} from spawn {0}", Id, UnitId);
                return null;
            }

            npc.Spawn();
            _lastSpawn = npc;
            _spawned.Add(npc);
            _scheduledCount--;
            _spawnCount++;
            if (npc.TemplateId == 3492 || npc.TemplateId == 3475 || npc.TemplateId == 3464 || npc.TemplateId == 916 || npc.TemplateId == 11951 || npc.TemplateId == 7674 || npc.TemplateId == 7648 || npc.TemplateId == 7677 || npc.TemplateId == 7676 || npc.TemplateId == 7673 || npc.TemplateId == 4499 || npc.TemplateId == 4498 || npc.TemplateId == 4500 || npc.TemplateId == 3451)
            {
                Patrol patrol = null;
                var rnd = Rand.Next(0, 1000);
                if (rnd > 500)
                {
                    patrol = new Square { Interrupt = true, Loop = true, Abandon = false };
                }
                else
                {
                    patrol = new Circular {Interrupt = true, Loop = true, Abandon = false};
                }
                patrol.Pause(npc);
                npc.Patrol = patrol;
                npc.Patrol.LastPatrol = patrol;
                patrol.Recovery(npc);
            }
           
            return npc;
        }

    public override void Despawn(Npc npc)
    {
        npc.Delete();
        if (npc.Respawn == DateTime.MinValue)
        {
            _spawned.Remove(npc);
            ObjectIdManager.Instance.ReleaseId(npc.ObjId);
            _spawnCount--;
        }

        if (_lastSpawn == null || _lastSpawn.ObjId == npc.ObjId)
        {
            _lastSpawn = _spawned.Count != 0 ? _spawned[_spawned.Count - 1] : null;
        }
    }

    public void DecreaseCount(Npc npc)
    {
        _spawnCount--;
        _spawned.Remove(npc);
        if (RespawnTime > 0 && (_spawnCount + _scheduledCount) < Count)
        {
            npc.Respawn = DateTime.Now.AddSeconds(RespawnTime);
            SpawnManager.Instance.AddRespawn(npc);
            _scheduledCount++;
        }

        npc.Despawn = DateTime.Now.AddSeconds(DespawnTime);
        SpawnManager.Instance.AddDespawn(npc);
    }
}
}
