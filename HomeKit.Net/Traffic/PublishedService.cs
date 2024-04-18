namespace HomeKit.Net.Traffic
{
    public class PublishedService
    {
        public IPublishClient Client;
        public string LongName;         // "745E1C22FAFD@Living Room"
        public string ShortName;        // "Living-Room"
        public string ServiceType;      // "_raop._tcp.local."
        public ushort Port;
        public Dictionary<string, string> TxtRecord;
    }
}
