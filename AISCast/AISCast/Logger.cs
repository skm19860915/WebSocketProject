using AISCast.Model.Message;
using System;
using System.IO;

namespace AISCast
{
    public static class Logger
    {
        private static object _connectionLogLock = new object();
        private static object _aisFileLock = new object();
        private static object _crashLogLock = new object();

        //TEMP
        private static object _rawLock = new object();
        private static object _vesselMessageLock = new object();

        public static void WriteRawMessage(string message, string name)
        {
            lock (_rawLock)
            {
                File.AppendAllText("rawmessage.log", string.Format("{0}\t__{1}__\t{2}\r\n", DateTime.Now, message, name));
            }
        }

        public static void WriteVesselMessage(string name, Message5 message, string rawMessage)
        {
            lock(_vesselMessageLock)
            {
                File.AppendAllText("vesselmessage.log", string.Format("{0}\t{1}\t{2}_{3}_{4}\t{5}\r\n", DateTime.Now, name, message.MMSI, message.CallSign, message.VesselName, rawMessage));
            }
        }

        public static void WriteLog(string message)
        {
            var dateNow = DateTime.Now;
            var date = dateNow.ToString("yyyy-MM-dd");
            var time = dateNow.ToString("hh:mm:ss");
            var timestamp = string.Format("{0} {1}", date, time);
            var formattedMessage = string.Format("{0} - {1}", timestamp, message);

            Console.WriteLine(formattedMessage);
            WriteFile(date, formattedMessage);
        }

        public static void WriteAISTrackData(string message)
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd");
            var filename = string.Format("{0}-AIS-LOG.log", date);

            lock (_aisFileLock)
            {
                File.AppendAllText(filename, string.Format("{0}\r\n", message));
            }
        }

        public static void WriteCrashLog(Exception e, bool crashed = false)
        {
            lock (_crashLogLock)
            {
                File.AppendAllText("crashlog.log", string.Format("{0}\t{1}\r\n", DateTime.Now, e.ToString()));
                if (e.InnerException == null)
                {
                    File.AppendAllText("crashlog.log", crashed ? "=========CRASHED==========" : "=========ERROR===========");
                }
                else
                {
                    File.AppendAllText("crashlog.log", "== INNER ==");
                    WriteCrashLog(e, crashed);
                }
            }
        }

        private static void WriteFile(string date, string message)
        {
            var filename = string.Format("{0}-CONNECTION-LOG.log", date);

            lock (_connectionLogLock)
            {
                File.AppendAllText(filename, string.Format("{0}\r\n", message));
            }
        }
    }
}
