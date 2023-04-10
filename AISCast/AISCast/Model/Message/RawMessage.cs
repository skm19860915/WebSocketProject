namespace AISCast.Model.Message
{
    public class RawMessage
    {
        public string Id { get; set; }
        public int Length { get; set; }
        public int Offset { get; set; }
        public string Channel { get; set; }
        public string Payload { get; set; }
        public int Padding { get; set; }
        public string Checksum { get; set; }

        public RawMessage(string message, string name)
        {
            Logger.WriteRawMessage(message, name);
            try
            {
                var split = message.Split(',');

                Id = split[0];

                Length = int.Parse(split[1]);
                Offset = int.Parse(split[2]);
                Channel = split[4];
                Payload = split[5];

                var endSplit = split[6].Split('*');

                Padding = int.Parse(endSplit[0]);
                Checksum = endSplit[1];
            }
            catch { }
        }
    }
}
