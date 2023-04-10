using System.Collections.Generic;

namespace AISCast.Configuration
{
    public class Config
    {
        public string BaseUrl { get; set; }
        public int ShowBroadcastMessages { get; set; }
        public int ShowVesselMessages
        {
            get { return InternalShowVesselMessages ? 1 : 0; }
            set { InternalShowVesselMessages = value.Equals(1); }
        }
        public int UidValidationTimeout { get; set; }
        public int ActiveConnectionInterval { get; set; }
        public int LocationUpdateInterval { get; set; }
        public List<Endpoint> AntennaEndpoints { get; set; }
        public Endpoint BroadcastEndpoint { get; set; }
        public List<WhitelistEntry> WhitelistEntries { get; set; }

        public static bool InternalShowVesselMessages { get; set; }
    }
}
