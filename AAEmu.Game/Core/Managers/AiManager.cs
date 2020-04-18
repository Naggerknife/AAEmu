using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Network.Connections;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.Game.Duels;
using AAEmu.Game.Models.Game.Error;
using AAEmu.Game.Models.Game.Units.Route;
using AAEmu.Game.Models.Tasks.Doodads;
using AAEmu.Game.Models.Tasks.Duels;
using AAEmu.Game.Models.Tasks.UnitMove;
using AAEmu.Game.Utils;
using NLog;

namespace AAEmu.Game.Core.Managers
{
    public class AiManager : Singleton<AiManager>
    {
        protected static Logger _log = LogManager.GetCurrentClassLogger();

        public List<string> RecordPath;   //  данные для записи пути
        public List<string> MovePath;     //  данные по которому мы будем двигатся в данный момент
        public bool SavePathEnabled;      // флаг записи пути
        public Simulation _sim;

        protected AiManager()
        {
            RecordPath = new List<string>();
            MovePath = new List<string>();
            _sim = null;
        }

        public bool Initialize()
        {
            _log.Info("Initialising AI Manager...");
            return true;
        }
        public Simulation SetSim(Character ch)
        {
            _log.Info("Initialising [MoveTo]...");
            //CharacterManager.Instance.LoadCharacters()
            //_sim = new Simulation(Network.Connections.GameConnection.ActiveChar();
            return _sim;
        }
    }
}
