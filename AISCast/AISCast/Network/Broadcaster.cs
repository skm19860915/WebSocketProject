using AISCast.Configuration;
using AISCast.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace AISCast.Network
{
    public class Broadcaster
    {
        private string _ipAddress;
        private int _port;
        private const string DELIMITER = "\r\n";
        private ManualResetEvent _resetEvent;
        private List<SocketState> _activeSockets;
        private int _activateConnectionInterval;
        private Dictionary<string, AntennaListener> _listeners;

        public System.Windows.Forms.TextBox b_textbox;
        public SynchronizationContext b_context;

        public Broadcaster(string ipAddress, int port, int activeConnectionInterval, 
           Dictionary<string, AntennaListener> listeners)
        {
            _ipAddress = ipAddress;
            _port = port;
            _activeSockets = new List<SocketState>();
            _resetEvent = new ManualResetEvent(false);
            _activateConnectionInterval = activeConnectionInterval;
            _listeners = listeners;
        }

        public void Broadcast(string name, string message)
        {
            var deadSockets = new List<SocketState>();
            var messageBuffer = Encoding.UTF8.GetBytes(string.Concat(message, DELIMITER));

            foreach(var socketState in _activeSockets)
            {
                var socket = socketState.Socket;
                if (!socket.Connected)
                {
                    deadSockets.Add(socketState);
                    continue;
                }

                if (socketState.Established)
                    socket.Send(messageBuffer);
            }

            _activeSockets.RemoveAll(s => deadSockets.Contains(s));
        }

        private void SetTextSafePost(object text)
        {
            b_textbox.AppendText(text.ToString() + Environment.NewLine);
        }

        public void RunThreaded(SynchronizationContext context, System.Windows.Forms.TextBox tb)
        {
            b_textbox = tb;
            b_context = context;
            new Thread(new ThreadStart(Run)).Start();
            new Thread(new ThreadStart(ActiveConnectionsThread)).Start();
        }

        public void RunThreaded()
        {
            new Thread(new ThreadStart(Run)).Start();
            new Thread(new ThreadStart(ActiveConnectionsThread)).Start();
        }

        private void Run()
        {
            var ipe = new IPEndPoint(IPAddress.Parse(_ipAddress), _port);
            var socket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            socket.Bind(ipe);
            socket.Listen(12);
            
            while(true)
            {
                _resetEvent.Reset();

                socket.BeginAccept(new AsyncCallback(AcceptCallback), socket);

                _resetEvent.WaitOne();
            }
        }

        private void ActiveConnectionsThread()
        {
            while(true)
            {
                Thread.Sleep(_activateConnectionInterval);

                var activeSocketsCopy = _activeSockets.ToList();
                var headerString = string.Format("\r\nActive connections as of {0}:", DateTime.Now.ToString("yyyy-MM-dd hh:mm"));
                Console.WriteLine(headerString);
                b_context.Post(SetTextSafePost, headerString);

                if (activeSocketsCopy.Count == 0)
                {
                    Console.WriteLine("None");
                    b_context.Post(SetTextSafePost, "None");
                }

                foreach(var activeSocket in activeSocketsCopy)
                {
                    Console.WriteLine("{0}", activeSocket == null ? "SOCKET INVALID" : activeSocket.WhitelistName);
                    var invalid = string.Format("{0}", activeSocket == null ? "SOCKET INVALID" : activeSocket.WhitelistName);
                    b_context.Post(SetTextSafePost, invalid);

                    if (activeSocket == null)
                        Logger.WriteCrashLog(new Exception("activeSocket is null"));
                }

                Console.WriteLine();
            }
        }

        private void AcceptCallback(IAsyncResult result)
        {
            _resetEvent.Set();

            var socket = result.AsyncState as Socket;
            var newSocket = socket.EndAccept(result);

            var socketState = new SocketState
            {
                Socket = newSocket,
                IPAddress = (newSocket.RemoteEndPoint as IPEndPoint).Address.ToString()
            };

            WhitelistEntry whitelistEntry;

            socketState.Validated = SecurityValidator.IsWhitelistedIP(socketState.IPAddress, out whitelistEntry);

            if (socketState.Validated)
            {
                var message = string.Format("{0} [{1}]", whitelistEntry.Name, socketState.IPAddress);
                socketState.WhitelistName = message;
                socketState.Established = true;
                Logger.WriteLog(message);
            }
            else
            {
                socketState.StartCountdown();
            }

            var text = Encoding.UTF8.GetBytes("This is a test message sent from AIS");

            newSocket.Send(text);
            newSocket.BeginReceive(socketState.Buffer, 0, SocketState.BUFFER_SIZE, 0, new AsyncCallback(ReceiveCallback), socketState);

            _activeSockets.Add(socketState);
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            var socketState = result.AsyncState as SocketState;
            var socket = socketState.Socket;

            try
            {
                if (!socket.Connected)
                {
                    _activeSockets.Remove(socketState);
                    Console.WriteLine("{0} disconnected", socketState.IPAddress);
                    var disconnected1 = string.Format("{0} disconnected", socketState.IPAddress);
                    b_context.Post(SetTextSafePost, disconnected1);

                    return;
                }

                var read = socket.EndReceive(result);

                if (read > 0)
                {
                    socketState.Message.Append(Encoding.UTF8.GetString(socketState.Buffer, 0, read));
                }
                else
                {
                    socket.Close();
                    _activeSockets.Remove(socketState);
                    Console.WriteLine("{0} disconnected", socketState.IPAddress);
                    var disconnected2 = string.Format("{0} disconnected", socketState.IPAddress);
                    b_context.Post(SetTextSafePost, disconnected2);
                    return;
                }

                if (!socketState.Established && socketState.Message.Length >= 40)
                {
                    var uid = socketState.Message.ToString(0, 40);

                    var uidRegex = new Regex("[0-9a-fA-F]");
                    if (uidRegex.IsMatch(uid.Substring(0, 1)))
                    {
                        if (socketState.Validated)
                        {
                            socketState.Message.Remove(0, 40);
                        }
                        else
                        {
                            WhitelistEntry whitelistEntry;
                            if (!SecurityValidator.IsWhitelistedUID(uid, out whitelistEntry))
                            {
                                var message = string.Format("UNAUTHORIZED [Attempted: {0}; {1}]", socketState.IPAddress, uid);
                                Logger.WriteLog(message);
                                socket.Close();
                                return;
                            }
                            else
                            {
                                var message = string.Format("{0} [{1}]", whitelistEntry.Name, uid);
                                socketState.WhitelistName = message;
                                Logger.WriteLog(message);
                                socketState.Message.Remove(0, 40);
                            }
                        }
                    }

                    socketState.Established = true;
                }

                ProcessMessages(socketState.Message);

                socket.BeginReceive(socketState.Buffer, 0, SocketState.BUFFER_SIZE, 0, new AsyncCallback(ReceiveCallback), socketState);
            }
            catch(SocketException e)
            {
                _activeSockets.Remove(socketState);
                Console.WriteLine("{0} disconnected", socketState.IPAddress);
                var disconnected3 = string.Format("{0} disconnected", socketState.IPAddress);
                b_context.Post(SetTextSafePost, disconnected3);
                return;
            }
        }

        private void ProcessMessages(StringBuilder messageBuilder)
        {
            while (true)
            {
                var messagesString = messageBuilder.ToString();
                var delimiterOffset = messagesString.IndexOf(DELIMITER);

                if (delimiterOffset < 0)
                    return;

                var message = messagesString.Substring(0, delimiterOffset);

                Console.WriteLine("RECEIVED: \"{0}\"", message.Replace("\r", "\\r").Replace("\n", "\\n"));
                var received = string.Format("RECEIVED: \"{0}\"", message.Replace("\r", "\\r").Replace("\n", "\\n"));
                b_context.Post(SetTextSafePost, received);

                messageBuilder.Remove(0, delimiterOffset + DELIMITER.Length);
            }
        }
    }
}