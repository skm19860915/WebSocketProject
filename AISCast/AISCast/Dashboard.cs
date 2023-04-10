using AISCast.Configuration;
using AISCast.Model;
using AISCast.Network;
using AISCast.Security;
using AISCast.UI;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace AISCast
{
    public partial class Dashboard : Form
    {
        SynchronizationContext m_SyncContext = null;

        private static Broadcaster Broadcaster;
        private static bool ShowBroadcastMessages;

        private static Dictionary<string, Sensor> Sensors;
        private static int LocationUpdateInterval;
        private static Dictionary<string, AntennaListener> Listeners;

        private static Config _config;
        private static string _xmlFile = @"configuration.xml";

        private IDictionary<string, int> _tracks;

        public Dashboard()
        {
            InitializeComponent();
            m_SyncContext = SynchronizationContext.Current;
            CenterToScreen();
            currentTimer.Start();
            tracksTimer.Start();
        }

        private void Dashboard_Load(object sender, EventArgs e)
        {
            LoadConfiguration();
            loadConfigData();
            loadConnectionStatusAsync();
        }

        private List<LocationRecord> getLocationList(List<Endpoint> endpoints, IDictionary<string, int> tracks)
        {
            var list = new List<LocationRecord>();
            endpoints.ForEach(x => list.Add(new LocationRecord()
            {
                Name = x.Name,
                Host = x.Host,
                Port = x.Port,
                Tracks = tracks == null ? 0 : tracks.FirstOrDefault(y => string.Equals(y.Key, x.Name)).Value
            }));

            return list;
        }

        private void loadConfigData()
        {
            var config = _config;

            gvLocation.DataSource = getLocationList(config.AntennaEndpoints, null);
            var ips = config.WhitelistEntries.Where(x => x.EntryType == WhitelistEntryType.IPAddress);
            gvWhiteListIPs.DataSource = ips.ToList();
            var uids = config.WhitelistEntries.Where(x => x.EntryType == WhitelistEntryType.UID);
            gvWhiteListUIDs.DataSource = uids.ToList();
            gvWhiteListUIDs.Columns["EntryType"].Visible = false;
            gvWhiteListIPs.Columns["EntryType"].Visible = false;
            lbServer.Text = "Server Address - " + config.BroadcastEndpoint.Host + " : " + config.BroadcastEndpoint.Port;
        }

        private void loadConnectionStatusAsync()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            var config = _config;

            Sensors = new Dictionary<string, Sensor>();
            LocationUpdateInterval = config.LocationUpdateInterval;

            UrlPoster.BaseURL = config.BaseUrl;

            var listeners = new Dictionary<string, AntennaListener>();

            foreach (var endpoint in config.AntennaEndpoints)
            {
                var listener = new AntennaListener(endpoint.Name, endpoint.Host, endpoint.Port);
                listener.MessageReceived += Listener_MessageReceived;
                listener.VesselEventReceived += Listener_VesselEventReceived;
                //listener.RunThreaded();
                listener.RunThreaded(m_SyncContext, tbConnected, tbDataRaw, lbProcessedTime);

                listeners.Add(endpoint.Name, listener);

                var sensor = new Sensor
                {
                    Name = listener.Name
                };

                Sensors.Add(listener.Name, sensor);
            }

            Listeners = listeners;

            ShowBroadcastMessages = config.ShowBroadcastMessages == 1;

            SecurityValidator.SetSecurity(config.WhitelistEntries, config.UidValidationTimeout);

            Broadcaster = new Broadcaster(config.BroadcastEndpoint.Host,
                config.BroadcastEndpoint.Port,
                config.ActiveConnectionInterval,
                listeners);

            //Broadcaster.RunThreaded();
            Broadcaster.RunThreaded(m_SyncContext, tbConnected);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.WriteCrashLog(e.ExceptionObject as Exception, true);
        }

        private static void Listener_VesselEventReceived(string name, string message)
        {
            LogStatistics(name, message);
        }

        private static void Listener_MessageReceived(string name, string message)
        {
            Broadcaster.Broadcast(name, message);

            if (ShowBroadcastMessages)
                Console.WriteLine("Broadcast: {0}", message);
        }

        private void LoadConfiguration()
        {
            var xml = File.ReadAllText(_xmlFile);

            var serializer = new XmlSerializer(typeof(Config));
            var reader = new StringReader(xml);
            var configuration = serializer.Deserialize(reader) as Config;

            _tracks = TrackAssignment.trackList;
            foreach(var item in configuration.AntennaEndpoints)
            {
                _tracks.Add(item.Name, 0);
            }

            _config = configuration;
        }

        private static void LogStatistics(string name, string vesselName)
        {
            var now = DateTime.Now;
            lock (Sensors)
            {
                if (!Sensors.ContainsKey(name))
                    Sensors.Add(name, new Sensor
                    {
                        Name = name
                    });

                var sensor = Sensors[name];

                if (!sensor.Statistics.ContainsKey(vesselName))
                {
                    sensor.Statistics.Add(vesselName, new List<DateTime>());
                }
                sensor.Statistics[vesselName].Add(now);
            }
        }

        private static void CleanupStatistics()
        {
            var windowStart = DateTime.Now.AddMilliseconds(-LocationUpdateInterval);
            var deadLocations = new List<string>();

            lock (Sensors)
            {
                foreach (var sensorKv in Sensors)
                {
                    var sensor = sensorKv.Value;
                    var keysToRemove = new List<string>();
                    foreach (var statisticsKv in sensor.Statistics)
                    {
                        var vesselStats = statisticsKv.Value;
                        vesselStats.RemoveAll(dt => dt < windowStart);

                        if (vesselStats.Count == 0)
                            keysToRemove.Add(statisticsKv.Key);
                    }

                    foreach (var keyToRemove in keysToRemove)
                        sensor.Statistics.Remove(keyToRemove);

                    sensor.Tracks = sensor.Statistics.Count;
                }
            }
        }

        private void btnLocationAdd_Click(object sender, EventArgs e)
        {
            var dialog = new AddDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
                addRow(dialog.DeviceName, dialog.DeviceId, 1);
        }

        private void btnAddIP_Click(object sender, EventArgs e)
        {
            var dialog = new AddDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
                addRow(dialog.DeviceName, dialog.DeviceId, 2);
        }

        private void btnAddUID_Click(object sender, EventArgs e)
        {
            var dialog = new AddDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
                addRow(dialog.DeviceName, dialog.DeviceId, 3);
        }

        private void addRow(string name, string id, int index)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(id))
                return;

            var config = _config;

            switch (index)
            {
                case 1:
                    insertLocation(name, id, config);
                    break;
                case 2:
                    insertWhiteListIP(name, id, config);
                    break;
                case 3:
                    insertWhiteListUID(name, id, config);
                    break;
            }

            var file = File.Create(_xmlFile);
            var serializer = new XmlSerializer(typeof(Config));
            serializer.Serialize(file, _config);
            file.Close();
        }

        private void insertLocation(string name, string id, Config config)
        {
            var lastElementPort = config.AntennaEndpoints.Last().Port;
            config.AntennaEndpoints.Add(new Endpoint()
            {
                Name = name,
                Host = id,
                Port = lastElementPort + 1
            });

            _tracks.Add(name, 0);

            _config = config;
            gvLocation.DataSource = getLocationList(config.AntennaEndpoints, _tracks);
            gvLocation.Refresh();
        }

        private void insertWhiteListIP(string name, string id, Config config)
        {
            config.WhitelistEntries.Add(new WhitelistEntry()
            {
                EntryType = WhitelistEntryType.IPAddress,
                Name = name,
                Id = id
            });

            _config = config;
            var ips = config.WhitelistEntries.Where(x => x.EntryType == WhitelistEntryType.IPAddress);
            gvWhiteListIPs.DataSource = ips.ToList();
            gvWhiteListIPs.Refresh();
        }

        private void insertWhiteListUID(string name, string id, Config config)
        {
            config.WhitelistEntries.Add(new WhitelistEntry()
            {
                EntryType = WhitelistEntryType.UID,
                Name = name,
                Id = id
            });

            _config = config;
            var uids = config.WhitelistEntries.Where(x => x.EntryType == WhitelistEntryType.UID);
            gvWhiteListUIDs.DataSource = uids.ToList();
            gvWhiteListUIDs.Refresh();
        }

        private void btnDeleteLocation_Click(object sender, EventArgs e)
        {
            if (gvLocation == null || gvLocation.CurrentCell == null)
                return;

            var index = gvLocation.CurrentCell.RowIndex;
            deleteRow(index, 1);
        }

        private void btnRemoveIP_Click(object sender, EventArgs e)
        {
            if (gvWhiteListIPs == null || gvWhiteListIPs.CurrentCell == null)
                return;

            var index = gvWhiteListIPs.CurrentCell.RowIndex;
            deleteRow(index, 2);
        }

        private void btnRemoveUID_Click(object sender, EventArgs e)
        {
            if (gvWhiteListUIDs == null || gvWhiteListUIDs.CurrentCell == null)
                return;

            var index = gvWhiteListUIDs.CurrentCell.RowIndex;
            deleteRow(index, 3);
        }

        private void deleteRow(int rowIndex, int index)
        {
            var config = _config;

            switch (index)
            {
                case 1:
                    deleteLocation(rowIndex, config);
                    break;
                case 2:
                    deleteWhiteListIP(rowIndex, config);
                    break;
                case 3:
                    deleteWhiteListUID(rowIndex, config);
                    break;
            }

            var file = File.Create(_xmlFile);
            var serializer = new XmlSerializer(typeof(Config));
            serializer.Serialize(file, _config);
            file.Close();
        }

        private void deleteLocation(int rowIndex, Config config)
        {
            var delLocation = config.AntennaEndpoints.ElementAt(rowIndex);
            _tracks.Remove(delLocation.Name);
            config.AntennaEndpoints.RemoveAt(rowIndex);

            gvLocation.DataSource = getLocationList(config.AntennaEndpoints, _tracks);
            gvLocation.Refresh();
            _config = config;
        }

        private void deleteWhiteListIP(int rowIndex, Config config)
        {
            var ips = config.WhitelistEntries.Where(x => x.EntryType == WhitelistEntryType.IPAddress).ToList();
            ips.RemoveAt(rowIndex);
            gvWhiteListIPs.DataSource = ips.ToList();
            gvWhiteListIPs.Refresh();

            config.WhitelistEntries.RemoveAll(x => x.EntryType == WhitelistEntryType.IPAddress);
            config.WhitelistEntries.AddRange(ips);
            _config = config;
        }

        private void deleteWhiteListUID(int rowIndex, Config config)
        {
            var uids = config.WhitelistEntries.Where(x => x.EntryType == WhitelistEntryType.UID).ToList();
            uids.RemoveAt(rowIndex);
            gvWhiteListUIDs.DataSource = uids.ToList();
            gvWhiteListUIDs.Refresh();

            config.WhitelistEntries.RemoveAll(x => x.EntryType == WhitelistEntryType.UID);
            config.WhitelistEntries.AddRange(uids);
            _config = config;
        }

        private void Dashboard_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
                Environment.Exit(0);
        }

        private void currentTimer_Tick(object sender, EventArgs e)
        {
            var datetime = DateTime.Now;
            lbCurrentTime.Text = "Current Time : " + datetime.ToString();
        }

        private void tracksTimer_Tick(object sender, EventArgs e)
        {
            var config = _config;
            var list = getLocationList(config.AntennaEndpoints, _tracks);
            gvLocation.DataSource = list;
            gvLocation.Refresh();
        }
    }
}
