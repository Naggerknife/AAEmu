using System.Drawing;
using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C
{
    public class SCNoticeMessagePacket : GamePacket
    {

        //Initialize
        readonly string _message = "";
        readonly byte _type = 3;
        readonly string _alphahex = "FF";
        readonly string _colorhex = "80FF80";
        readonly int _vistime = 1000;

        public SCNoticeMessagePacket(byte type, Color argbColor, int vistime, string message) : base(SCOffsets.SCNoticeMessagePacket, 1)
        {
            // Set Opacity to max if none was provided
            if (argbColor.A <= 0)
                argbColor = Color.FromArgb(0xFF, argbColor.R, argbColor.G, argbColor.B);
            // if no visible time set, generate automatic timing
            if (vistime <= 0)
                vistime = 1000 + (message.Length * 50);
            _type = type;
            _alphahex = argbColor.A.ToString("X2");
            _colorhex = argbColor.R.ToString("X2") + argbColor.G.ToString("X2") + argbColor.B.ToString("X2");
            _vistime = vistime;
            _message = message;
        }

        public override PacketStream Write(PacketStream stream)
        {
            stream.Write(_type); // noticeType (1 - 3)
            stream.Write(_alphahex); // A-component of ARGB color value
            stream.Write(_vistime); // visibleTime (Miliseconds)
            stream.Write(_colorhex + _message); // RGB-component of ARGB color value with added message

            return stream;
        }
    }
}
