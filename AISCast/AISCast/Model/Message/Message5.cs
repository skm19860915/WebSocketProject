namespace AISCast.Model.Message
{
    public class Message5 : IMessage
    {
        public string CallSign { get; set; }
        public string VesselName { get; set; }
        public string MMSI { get; set; }
    }
}
