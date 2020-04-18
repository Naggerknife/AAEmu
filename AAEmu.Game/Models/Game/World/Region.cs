using System;
using System.Collections.Generic;
using System.Linq;

using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.Game.Housing;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.Units.Route;

using NLog;

namespace AAEmu.Game.Models.Game.World
{
    public class Region
    {
        private static Logger _log = LogManager.GetCurrentClassLogger();

        private readonly uint _worldId;
        private readonly object _objectsLock = new object();
        private GameObject[] _objects;
        private int _objectsSize, _charactersSize;

        public int X { get; }
        public int Y { get; }
        public int Id => Y + 1024 * X;

        public Region(uint worldId, int x, int y)
        {
            _worldId = worldId;
            X = x;
            Y = y;
        }

        public void AddObject(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }

            lock (_objectsLock)
            {
                if (_objects == null)
                {
                    _objects = new GameObject[50];
                    _objectsSize = 0;
                }
                else if (_objectsSize >= _objects.Length)
                {
                    var temp = new GameObject[_objects.Length * 2];
                    Array.Copy(_objects, 0, temp, 0, _objectsSize);
                    _objects = temp;
                }

                _objects[_objectsSize] = obj;
                _objectsSize++;

                obj.Position.WorldId = _worldId;
                var zoneId = WorldManager.Instance.GetZoneId(_worldId, obj.Position.X, obj.Position.Y);
                if (zoneId > 0)
                {
                    obj.Position.ZoneId = zoneId;
                }

                if (obj is Character)
                {
                    _charactersSize++;
                }
            }
        }

        public void RemoveObject(GameObject obj) // TODO Нужно доделать =_+
        {
            if (obj == null)
            {
                return;
            }
            lock (_objectsLock)
            {
                if (_objects == null || _objectsSize == 0)
                {
                    return;
                }
                if (_objectsSize > 1)
                {
                    var index = -1;
                    for (var i = 0; i < _objects.Length; i++)
                    {
                        if (_objects[i] != obj)
                        {
                            continue;
                        }
                        index = i;
                        break;
                    }
                    if (index > -1)
                    {
                        _objects[index] = _objects[_objectsSize - 1];
                        _objects[_objectsSize - 1] = null;
                        _objectsSize--;
                    }
                }
                else if (_objectsSize == 1 && _objects[0] == obj)
                {
                    _objects[0] = null;
                    _objects = null;
                    _objectsSize = 0;
                }
                if (obj is Character)
                {
                    _charactersSize--;
                }
            }
        }

        public void AddToCharacters(GameObject obj)
        {
            if (_objects == null)
            {
                return;
            }
            // показать игроку все объекты в регионе
            if (obj is Character character1)
            {
                var units = GetList(new List<Unit>(), character1.ObjId);
                foreach (var t in units)
                {
                    if (t is Npc npc)
                    {
                        // включаем движение видимых NPC
                        //if (npc.TemplateId == 3492 || npc.TemplateId == 3475 || npc.TemplateId == 3464 ||
                        //    npc.TemplateId == 916 || npc.TemplateId == 11951 || npc.TemplateId == 7674 ||
                        //    npc.TemplateId == 7648 || npc.TemplateId == 7677 || npc.TemplateId == 7676 ||
                        //    npc.TemplateId == 7673 || npc.TemplateId == 4499 || npc.TemplateId == 4498 ||
                        //    npc.TemplateId == 4500 || npc.TemplateId == 3451 || npc.TemplateId == 3502 ||
                        //    npc.TemplateId == 3494 || npc.TemplateId == 3480 || npc.TemplateId == 3460 ||
                        //    npc.TemplateId == 3495)
                        if (npc.Faction.GuardHelp == false)
                        {
                            if (npc.Patrol == null)
                            {
                                Patrol patrol = null;
                                var rnd = Rand.Next(0, 600);
                                if (rnd > 510)
                                {
                                    //NPC медленно шевелятся
                                   var stirring = new Stirring() { Interrupt = true, Loop = true, Abandon = false };
                                    stirring.Degree = (short)Rand.Next(180, 360);
                                    patrol = stirring;
                                }
                                else if (rnd > 410)
                                {
                                    // NPC движутся по квадрату
                                    var square = new Square() { Interrupt = true, Loop = true, Abandon = false };
                                    square.Degree = (short)Rand.Next(180, 360);
                                    patrol = square;
                                }
                                else if (rnd > 310)
                                {
                                    // NPC движутся по кругу
                                    patrol = new Circular() { Interrupt = true, Loop = true, Abandon = false };
                                }
                                else if (rnd > 210)
                                {
                                    //NPC дерганно двигаются
                                    var jerky = new Jerky { Interrupt = true, Loop = true, Abandon = false };
                                    jerky.Degree = (short)Rand.Next(180, 360);
                                    patrol = jerky;
                                }
                                else if (rnd > 110)
                                {
                                    //NPC движутся по ткацкому челноку по оси Y
                                    var quill = new QuillY { Interrupt = true, Loop = true, Abandon = false };
                                    quill.Degree = (short)Rand.Next(180, 360);
                                    patrol = quill;
                                }
                                else if (rnd > 10)
                                {
                                    //NPC движутся по ткацкому челноку по оси Y
                                    var quill = new QuillX { Interrupt = true, Loop = true, Abandon = false };
                                    quill.Degree = (short)Rand.Next(180, 360);
                                    patrol = quill;
                                }
                                else if (rnd <= 10)
                                {
                                    // NPC стоят на месте
                                    npc.Patrol = null;
                                }
                                if (patrol != null)
                                {
                                    patrol.Pause(npc);
                                    npc.Patrol = patrol;
                                    npc.Patrol.LastPatrol = null;
                                    patrol.Recovery(npc);
                                }
                            }
                        }
                        character1.SendPacket(new SCUnitStatePacket(npc));
                    }
                    else
                    {
                        character1.SendPacket(new SCUnitStatePacket(t));
                        if (t is House house)
                        {
                            character1.SendPacket(new SCHouseStatePacket(house));
                        }
                    }
                }
                var doodads = GetList(new List<Doodad>(), character1.ObjId).ToArray();
                for (var i = 0; i < doodads.Length; i += 30)
                {
                    var count = doodads.Length - i;
                    var temp = new Doodad[count <= 30 ? count : 30];
                    Array.Copy(doodads, i, temp, 0, temp.Length);
                    character1.SendPacket(new SCDoodadsCreatedPacket(temp));
                }
            }
            // показать объекты всем игрокам в регионе
            foreach (var character in GetList(new List<Character>(), obj.ObjId))
            {
                obj.AddVisibleObject(character);
            }
        }

        public void RemoveFromCharacters(GameObject obj)
        {
            if (_objects == null)
            {
                return;
            }
            // убрать у игрока все видимые объекты в регионе
            if (obj is Character character1)
            {
                var unitIds = GetListId<Unit>(new List<uint>(), character1.ObjId).ToArray();
                var units = GetList(new List<Unit>(), character1.ObjId);
                foreach (var t in units)
                {
                    if (t is Npc npc)
                    {
                        npc.Patrol = null;
                    }
                }

                for (var offset = 0; offset < unitIds.Length; offset += 500)
                {
                    var length = unitIds.Length - offset;
                    var temp = new uint[length > 500 ? 500 : length];
                    Array.Copy(unitIds, offset, temp, 0, temp.Length);
                    character1.SendPacket(new SCUnitsRemovedPacket(temp));
                }
                var doodadIds = GetListId<Doodad>(new List<uint>(), character1.ObjId).ToArray();
                for (var offset = 0; offset < doodadIds.Length; offset += 400)
                {
                    var length = doodadIds.Length - offset;
                    var last = length <= 400;
                    var temp = new uint[last ? length : 400];
                    Array.Copy(doodadIds, offset, temp, 0, temp.Length);
                    character1.SendPacket(new SCDoodadsRemovedPacket(last, temp));
                }
                // TODO ... others types...
            }
            // убрать объекты у всех игроков в регионе
            foreach (var character in GetList(new List<Character>(), obj.ObjId))
            {
                obj.RemoveVisibleObject(character);
            }
        }

        public Region[] GetNeighbors()
        {
            return WorldManager.Instance.GetNeighbors(_worldId, X, Y);
        }

        public bool AreNeighborsEmpty()
        {
            return IsEmpty() && GetNeighbors().All(neighbor => neighbor.IsEmpty());
        }

        public bool IsEmpty()
        {
            return _charactersSize <= 0;
        }

        public List<uint> GetObjectIdsList(List<uint> result, uint exclude)
        {
            GameObject[] temp;
            lock (_objectsLock)
            {
                if (_objects == null || _objectsSize == 0)
                {
                    return result;
                }
                temp = new GameObject[_objectsSize];
                Array.Copy(_objects, 0, temp, 0, _objectsSize);
            }
            result.AddRange(from obj in temp where obj.ObjId != exclude select obj.ObjId);
            return result;
        }

        public List<GameObject> GetObjectsList(List<GameObject> result, uint exclude)
        {
            GameObject[] temp;
            lock (_objectsLock)
            {
                if (_objects == null || _objectsSize == 0)
                {
                    return result;
                }

                temp = new GameObject[_objectsSize];
                Array.Copy(_objects, 0, temp, 0, _objectsSize);
            }
            result.AddRange(temp.Where(obj => obj != null && obj.ObjId != exclude));
            return result;
        }

        public List<uint> GetListId<T>(List<uint> result, uint exclude) where T : class
        {
            GameObject[] temp;
            lock (_objectsLock)
            {
                if (_objects == null || _objectsSize == 0)
                {
                    return result;
                }
                temp = new GameObject[_objectsSize];
                Array.Copy(_objects, 0, temp, 0, _objectsSize);
            }

            result.AddRange(from obj in temp where obj is T && obj.ObjId != exclude select obj.ObjId);
            return result;
        }

        public List<T> GetList<T>(List<T> result, uint exclude) where T : class
        {
            GameObject[] temp;
            lock (_objectsLock)
            {
                if (_objects == null || _objectsSize == 0)
                {
                    return result;
                }
                temp = new GameObject[_objectsSize];
                Array.Copy(_objects, 0, temp, 0, _objectsSize);
            }
            result.AddRange(from obj in temp let item = obj as T where item != null && obj.ObjId != exclude select item);
            return result;
        }

        public List<T> GetList<T>(List<T> result, uint exclude, float x, float y, float sqrad) where T : class
        {
            GameObject[] temp;
            lock (_objectsLock)
            {
                if (_objects == null || _objectsSize == 0)
                {
                    return result;
                }
                temp = new GameObject[_objectsSize];
                Array.Copy(_objects, 0, temp, 0, _objectsSize);
            }
            foreach (var obj in temp)
            {
                if (!(obj is T item) || obj.ObjId == exclude)
                {
                    continue;
                }
                var dx = obj.Position.X - x;
                dx *= dx;
                if (dx > sqrad)
                {
                    continue;
                }
                var dy = obj.Position.Y - y;
                dy *= dy;
                if (dx + dy < sqrad)
                {
                    result.Add(item);
                }
            }
            return result;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj.GetType() != typeof(Region))
            {
                return false;
            }
            var other = (Region)obj;
            return other._worldId == _worldId && other.X == X && other.Y == Y;
        }

        public override int GetHashCode()
        {
            var result = (int)_worldId;
            result = (result * 397) ^ X;
            result = (result * 397) ^ Y;
            return result;
        }
    }
}
