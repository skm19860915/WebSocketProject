using AISCast.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace AISCast.Network
{
    public class SocketState
    {
        public Socket Socket = null;
        public const int BUFFER_SIZE = 1024;
        public byte[] Buffer = new byte[BUFFER_SIZE];
        public StringBuilder Message = new StringBuilder();
        public bool Validated = false;
        public bool Established = false;
        public string IPAddress = null;
        public string WhitelistName = null;

        public void StartCountdown()
        {
            new Thread(InternalCountdown).Start();
        }

        private void InternalCountdown()
        {
            Thread.Sleep(SecurityValidator.UidTimeout);
            if (!Established && !Validated && Socket.Connected)
            {
                var message = string.Format("UNAUTHORIZED [Attempted: {0}; no uid]", IPAddress);
                Logger.WriteLog(message);
                Socket.Close();
            }
        }
    }
}
