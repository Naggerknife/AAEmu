using NLog;
using System;

using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.Id;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Models.Game.Units.Movements;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.World;
using AAEmu.Game.Utils;
using AAEmu.Commons.Utils;
using AAEmu.Game.Models.Game.Units.Route;
using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Skills.Effects;
using AAEmu.Game.Models.Game.Skills.Templates;

namespace AAEmu.Game.Scripts.Commands
{
    public class MoveTo : ICommand
    {
        protected static Logger _log = LogManager.GetCurrentClassLogger();
        public void OnLoad()
        {
            CommandManager.Instance.Register("moveto", this);
        }

        public string GetCommandLineHelp()
        {
            return "<rec||save||go||back||stop>";
        }

        public string GetCommandHelpText()
        {
            return
                "what is he doing:\n" +
                "- automatically writes the route to the file;\n" +
                "- you can load path data from a file;\n- moves along the route.\n\n" +
                "To start, you need to create the route (s), recording occurs as follows:\n" +
                "1. Start recording;\n" +
                "2. Take a route;\n" +
                "3. Stop recording.\n" +
                "=== here is an example file structure (x, y, z) ===\n" +
                "| 19007 | 145255 | -3151 |\n" +
                "| 19016 | 144485 | -3130 |\n" +
                "| 19124 | 144579 | -3128 |\n" +
                "| 19112 | 144059 | -3103 |\n" +
                "===================================================;\n";
        }
        public void Execute(Character character, string[] args)
        {
            if (args.Length < 1)
            {
                character.SendMessage("[MoveTo] /moveto <rec||save||go||back||stop>");
                return;
            }
            var cmd = args[0];
            //var newY = float.Parse(args[1]);
            //var newZ = float.Parse(args[2]);

            //character.DisabledSetPosition = true;
            //character.SendPacket(new SCTeleportUnitPacket(0, 0, newX, newY, newZ, 0f));
            character.SendMessage("[MoveTo] cmd: {0}", cmd);
            var moveTo = new Simulation((Npc)character.CurrentTarget);

            switch (@cmd)
            {
                case "rec":
                    break;
                case "save":
                    break;
                case "go":
                    character.SendMessage("[MoveTo] бежим вперед");
                    moveTo.GoToPath((Npc)character.CurrentTarget, true);
                    break;
                case "back":
                    character.SendMessage("[MoveTo] бежим назад");
                    moveTo.GoToPath((Npc)character.CurrentTarget, false);
                    break;
                case "stop":
                    character.SendMessage("[MoveTo] стоим на месте");
                    moveTo.Stop((Npc)character.CurrentTarget);
                    break;
            }
        }
    }
}
