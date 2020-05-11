using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.Id;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Scripts.Commands
{
    public class TestTransfer : ICommand
    {
        public void OnLoad()
        {
            CommandManager.Instance.Register("test_transfer", this);
        }

        public string GetCommandLineHelp()
        {
            return "<id>";
        }

        public string GetCommandHelpText()
        {
            return "";
        }

        public void Execute(Character character, string[] args)
        {
            if (args.Length < 1)
            {
                character.SendMessage("[test_transfer] /test_transfer unitId (1..124)");
                return;
            }
            var transfer = new Transfer();

            switch (args[1])
            {
                case "1":
                    transfer.TemplateId = 1; // carriage_main
                    transfer.ModelId = 657;
                    break;
                case "2":
                    transfer.TemplateId = 2; // ferryboat
                    transfer.ModelId = 116;
                    break;
                case "3":
                    transfer.TemplateId = 3; // Marianople Circular Float Cockpit
                    transfer.ModelId = 606;
                    break;
                case "4":
                    transfer.TemplateId = 4; // Marianople Circulation Float
                    transfer.ModelId = 314;
                    break;
                case "back":
                    transfer.TemplateId = 5; // Wagon 1-1
                    transfer.ModelId = 565;
                    break;
                case "stop":
                    transfer.TemplateId = 6; // Salislead Peninsula ~ Liriot Hillside Loop Carriage
                    transfer.ModelId = 654;
                    break;
            }
            transfer.ObjId = ObjectIdManager.Instance.GetNextId();
            transfer.TlId = (ushort)TlIdManager.Instance.GetNextId();
            transfer.Level = 50;
            transfer.Position = character.Position.Clone();
            transfer.Position.X += 5f; // spawn_x_offset
            transfer.Position.Y += 5f; // spawn_Y_offset
            transfer.MaxHp = transfer.Hp = 5000;
            transfer.ModelParams = new UnitCustomModelParams();

            transfer.Spawn();
        }
    }
}
