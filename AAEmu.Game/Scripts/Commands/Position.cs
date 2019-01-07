using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;

namespace AAEmu.Game.Scripts.Commands
{
    public class Position : ICommand
    {
        public void OnLoad()
        {
            CommandManager.Instance.Register("position", this);
        }

        public void Execute(Character character, string[] args)
        {
            var position = character.CurrentTarget?.Position ?? character.Position;
            character.SendMessage("[Position] X: {0}, Y: {1}, Z: {2}", position.X, position.Y, position.Z);

            if (args.Length > 0)
                character.SendMessage("[Position] RotX: {0}, RotY: {1}, RotZ: {2}", position.RotationX, position.RotationY,
                    position.RotationY);
        }
    }
}