using System.Windows.Forms;

namespace AISCast
{
    class Program
    {
        //private static Broadcaster Broadcaster;
        //private static bool ShowBroadcastMessages;

        //private static Dictionary<string, Sensor> Sensors;
        //private static int LocationUpdateInterval;
        //private static Dictionary<string, AntennaListener> Listeners;

        static void Main(string[] args)
        {
            //AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            //var config = LoadConfiguration();

            //Sensors = new Dictionary<string, Sensor>();
            //LocationUpdateInterval = config.LocationUpdateInterval;

            //UrlPoster.BaseURL = config.BaseUrl;

            //var listeners = new Dictionary<string, AntennaListener>();

            //foreach(var endpoint in config.AntennaEndpoints)
            //{
            //    var listener = new AntennaListener(endpoint.Name, endpoint.Host, endpoint.Port);
            //    listener.MessageReceived += Listener_MessageReceived;
            //    listener.VesselEventReceived += Listener_VesselEventReceived;
            //    listener.RunThreaded();
            //    listeners.Add(endpoint.Name, listener);

            //    var sensor = new Sensor
            //    {
            //        Name = listener.Name
            //    };

            //    Sensors.Add(listener.Name, sensor);
            //}

            //Listeners = listeners;

            //ShowBroadcastMessages = config.ShowBroadcastMessages == 1;

            //SecurityValidator.SetSecurity(config.WhitelistEntries, config.UidValidationTimeout);

            //Broadcaster = new Broadcaster(config.BroadcastEndpoint.Host,
            //    config.BroadcastEndpoint.Port,
            //    config.ActiveConnectionInterval,
            //    listeners);

            //Broadcaster.RunThreaded();

            ////new Thread(new ThreadStart(LocationUpdateThread)).Start();

            //Console.ReadLine();
            Application.Run(new Dashboard());
        }

        //private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        //{
        //    Logger.WriteCrashLog(e.ExceptionObject as Exception, true);
        //}

        //private static void Listener_VesselEventReceived(string name, string message)
        //{
        //    LogStatistics(name, message);
        //}

        //private static void Listener_MessageReceived(string name, string message)
        //{
        //    Broadcaster.Broadcast(name, message);

        //    if (ShowBroadcastMessages)
        //        Console.WriteLine("Broadcast: {0}", message);
        //}

        //private static Config LoadConfiguration()
        //{
        //    var xml = File.ReadAllText(@"configuration.xml");

        //    var serializer = new XmlSerializer(typeof(Config));
        //    var reader = new StringReader(xml);
        //    var configuration = serializer.Deserialize(reader) as Config;

        //    return configuration;
        //}

        //private static void LogStatistics(string name, string vesselName)
        //{
        //    var now = DateTime.Now;
        //    lock (Sensors)
        //    {
        //        if (!Sensors.ContainsKey(name))
        //            Sensors.Add(name, new Sensor
        //            {
        //                Name = name
        //            });

        //        var sensor = Sensors[name];

        //        if (!sensor.Statistics.ContainsKey(vesselName))
        //        {
        //            sensor.Statistics.Add(vesselName, new List<DateTime>());
        //        }
        //        sensor.Statistics[vesselName].Add(now);
        //    }
        //}

        //private static void CleanupStatistics()
        //{
        //    var windowStart = DateTime.Now.AddMilliseconds(-LocationUpdateInterval);
        //    var deadLocations = new List<string>();

        //    lock (Sensors)
        //    {
        //        foreach (var sensorKv in Sensors)
        //        {
        //            var sensor = sensorKv.Value;
        //            var keysToRemove = new List<string>();
        //            foreach(var statisticsKv in sensor.Statistics)
        //            {
        //                var vesselStats = statisticsKv.Value;
        //                vesselStats.RemoveAll(dt => dt < windowStart);

        //                if (vesselStats.Count == 0)
        //                    keysToRemove.Add(statisticsKv.Key);
        //            }

        //            foreach (var keyToRemove in keysToRemove)
        //                sensor.Statistics.Remove(keyToRemove);

        //            sensor.Tracks = sensor.Statistics.Count;
        //        }
        //    }
        //}

        //private static void LocationUpdateThread()
        //{
        //    while (true)
        //    {
        //        Thread.Sleep(LocationUpdateInterval);

        //        CleanupStatistics();

        //        var headerString = string.Format("\r\nLocation update - {0}:", DateTime.Now.ToString("yyyy-MM-dd hh:mm"));

        //        Console.WriteLine(headerString);

        //        var locationStatsCopy = new List<Sensor>();

        //        lock (Sensors)
        //        {
        //            foreach (var kvPair in Sensors)
        //            {
        //                var sensor = kvPair.Value;
        //                sensor.Status = Listeners[sensor.Name].Status;
        //                locationStatsCopy.Add(kvPair.Value);
        //            }
        //        }

        //        foreach (var sensor in locationStatsCopy)
        //        {
        //            var locationString = string.Format("{0} - {1} - {2} Tracks", sensor.Name, sensor.Status, sensor.Tracks);
        //            Console.WriteLine(locationString);
        //        }

        //        Console.WriteLine();

        //        UrlPoster.PostData(DateTime.Now, locationStatsCopy);
        //    }
        //}

    }
}
