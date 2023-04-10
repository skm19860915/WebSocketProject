using AISCast.Configuration;
using AISCast.Model.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace AISCast.Network
{
    public delegate void MessageReceivedHandler(string name, string message);

    public class AntennaListener
    {
        private string _name;
        private string _ipAddress;
        private int _port;
        private const string DELIMITER = "\r\n";
        private const string VESSELMESSAGEID = "!AIVDM";
        private Dictionary<int, string> _vesselMessages;

        public string Name
        {
            get { return _name; }
        }

        public string Status { get; set; } = "Disconnected";
        public object Decode { get; private set; }

        public event MessageReceivedHandler MessageReceived;
        public event MessageReceivedHandler VesselEventReceived;

        public TextBox _textboxOfConnectedStatus;
        public TextBox _textboxOfDataRaw;
        public Label _labelOfProcessedNMEALineTime;
        public SynchronizationContext _context;
        public List<KeyValuePair<string, string>> listForTracks = new List<KeyValuePair<string, string>>();

        public AntennaListener(string name, string ipAddress, int port)
        {
            _name = name;
            _ipAddress = ipAddress;
            _port = port;
            _vesselMessages = new Dictionary<int, string>();
        }

        public void RunThreaded(SynchronizationContext context, TextBox tb1, TextBox tb2, Label lb)
        {
            _textboxOfConnectedStatus = tb1;
            _textboxOfDataRaw = tb2;
            _labelOfProcessedNMEALineTime = lb;
            var thread = new Thread(() => Run(context));
            thread.Start();
        }

        private void Run(SynchronizationContext context)
        {
            _context = context;

            var ipe = new IPEndPoint(IPAddress.Parse(_ipAddress), _port);
            var socket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            Console.WriteLine("Connecting to {0}:{1}", _ipAddress, _port);
            var connectingStatus = string.Format("Connecting to {0}:{1}", _ipAddress, _port);
            _context.Post(SetTextSafePost, connectingStatus);

            while (true)
            {
                try
                {
                    socket.Connect(ipe);

                    if (socket.Connected)
                    {
                        _vesselMessages.Clear();

                        Console.WriteLine("Connected to {0}:{1}", _ipAddress, _port);
                        var connectedStatus = string.Format("Connected to {0}:{1}", _ipAddress, _port);
                        _context.Post(SetTextSafePost, connectedStatus);

                        var socketState = new SocketState
                        {
                            Socket = socket
                        };

                        socket.BeginReceive(socketState.Buffer, 0, SocketState.BUFFER_SIZE, 0, new AsyncCallback(ReceiveCallback), socketState);

                        Status = "Connected";

                        return;
                    }
                }
                catch (SocketException e)
                {
                    Logger.WriteCrashLog(e);
                }

                Status = "Disconnected";

                Console.WriteLine("Connection to {0}:{1} failed... Sleeping...", _ipAddress, _port);
                var failedStatus = string.Format("Connection to {0}:{1} failed... Sleeping...", _ipAddress, _port);
                _context.Post(SetTextSafePost, failedStatus);
                Thread.Sleep(30000);
            }
        }

        private void SetTextSafePost(object text)
        {
            _textboxOfConnectedStatus.AppendText(text.ToString() + Environment.NewLine);
        }

        private void SetTextSafePostOfDataRaw(object text)
        {
            _textboxOfDataRaw.AppendText(text.ToString() + Environment.NewLine);
        }

        private void SetTextSafePostOfProcessedNMEALineTime(object text)
        {
            _labelOfProcessedNMEALineTime.Text = "Processed NMEA Line Time : " + text.ToString();
        }

        //public void RunThreaded()
        //{
        //    new Thread(new ThreadStart(Run)).Start();
        //}

        //private void Run()
        //{
        //    var ipe = new IPEndPoint(IPAddress.Parse(_ipAddress), _port);
        //    var socket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        //    Console.WriteLine("Connecting to {0}:{1}", _ipAddress, _port);

        //    while (true)
        //    {
        //        try
        //        {
        //            socket.Connect(ipe);

        //            if (socket.Connected)
        //            {
        //                _vesselMessages.Clear();

        //                Console.WriteLine("Connected to {0}:{1}", _ipAddress, _port);

        //                var socketState = new SocketState
        //                {
        //                    Socket = socket
        //                };

        //                socket.BeginReceive(socketState.Buffer, 0, SocketState.BUFFER_SIZE, 0, new AsyncCallback(ReceiveCallback), socketState);

        //                Status = "Connected";

        //                return;
        //            }
        //        }
        //        catch (SocketException e)
        //        {
        //            Logger.WriteCrashLog(e);
        //        }

        //        Status = "Disconnected";

        //        Console.WriteLine("Connection to {0}:{1} failed... Sleeping...", _ipAddress, _port);
        //        Thread.Sleep(30000);
        //    }
        //}

        private void ReceiveCallback(IAsyncResult result)
        {
            var socketState = result.AsyncState as SocketState;
            var socket = socketState.Socket;

            if (!socket.Connected)
            {
                Status = "Disconnected";
                Console.WriteLine("{0} connection lost", _name);
                var lostStatus1 = string.Format("{0} connection lost", _name);
                _context.Post(SetTextSafePost, lostStatus1);

                Run(_context);
                return;
            }

            try
            {
                var read = socket.EndReceive(result);

                if (read > 0)
                {
                    socketState.Message.Append(Encoding.UTF8.GetString(socketState.Buffer, 0, read));
                }
                else
                {
                    Status = "Disconnected";
                    socket.Close();
                    Console.WriteLine("{0} connection lost", _name);
                    var lostStatus2 = string.Format("{0} connection lost", _name);
                    _context.Post(SetTextSafePost, lostStatus2);
                    Run(_context);
                    return;
                }

                ProcessMessages(socketState.Message);

                socket.BeginReceive(socketState.Buffer, 0, SocketState.BUFFER_SIZE, 0, new AsyncCallback(ReceiveCallback), socketState);
            }
            catch(Exception e)
            {
                Logger.WriteCrashLog(e);
                Status = "Disconnected";
                socket.Close();
                Console.WriteLine("{0} connection lost", _name);
                var lostStatus3 = string.Format("{0} connection lost", _name);
                _context.Post(SetTextSafePost, lostStatus3);
                Run(_context);
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

                ProcessMessage(message);

                messageBuilder.Remove(0, delimiterOffset + DELIMITER.Length);
            }
        }

        private void ProcessMessage(string message)
        {
            MessageReceived?.Invoke(_name, message);

            Logger.WriteAISTrackData(message);

            if (message.StartsWith("!AIVDM"))
            {
                var parsedMessage = new RawMessage(message, _name);

                var rawData = string.Format("{0}\t__{1}__\t{2}\r\n", DateTime.Now, message, _name);
                _context.Post(SetTextSafePostOfDataRaw, rawData);
                _context.Post(SetTextSafePostOfProcessedNMEALineTime, DateTime.Now.ToString());

                var tracks = UI.TrackAssignment.trackList;
               

                foreach (var item in tracks.ToList())
                {
                    if(string.Equals(item.Key, _name))
                    {
                        var found = listForTracks.Any(x => string.Equals(x.Key, _name) && string.Equals(x.Value, message));
                        if(!found)
                            listForTracks.Add(new KeyValuePair<string, string>(_name, message));

                        var count = listForTracks.Where(x => string.Equals(x.Key, item.Key)).Count();
                        tracks[item.Key] = count;
                    }
                }

                if (parsedMessage.Id.Equals(VESSELMESSAGEID))
                {
                    if (!_vesselMessages.ContainsKey(parsedMessage.Offset))
                        _vesselMessages.Add(parsedMessage.Offset, parsedMessage.Payload);

                    if (_vesselMessages.Count == parsedMessage.Length)
                    {
                        var sortedKeys = _vesselMessages.OrderBy(kv => kv.Key).Select(kv => kv.Key).ToList();
                        var builder = new StringBuilder();

                        foreach (var key in sortedKeys)
                        {
                            builder.Append(_vesselMessages[key]);
                        }

                        DecodeVesselMessage(builder.ToString());
                        builder.Clear();
                        _vesselMessages.Clear();
                    }
                }
            }

        }

        private void DecodeVesselMessage(string message)
        {
            Message5 decodedMessage = null;

            try
            {
                decodedMessage = Decoding.Decoder.Decode(message) as Message5;
            }
            catch(Exception e)
            {
                Logger.WriteCrashLog(e);
            }

            if (decodedMessage == null)
                return;

            if (Config.InternalShowVesselMessages)
            {
                Console.WriteLine("Vessel message received from {0}: {1}", decodedMessage.MMSI, message);
                var receivedStatus = string.Format("Vessel message received from {0}: {1}", decodedMessage.MMSI, message);
                _context.Post(SetTextSafePost, receivedStatus);
                Logger.WriteVesselMessage(_name, decodedMessage, message);
            }

            VesselEventReceived?.Invoke(_name, decodedMessage.MMSI);
        }
    }
}
