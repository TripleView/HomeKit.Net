using System.Net;

namespace HomeKit.Net.Traffic
{
    public class ServiceResolveResult
    {
        public readonly string Name;
        public readonly string HostName;
        public readonly IPEndPoint[] IpEndpointList;
        public readonly IReadOnlyDictionary<string, string> TxtRecord;

        public ServiceResolveResult(
            string name,
            string hostName,
            IEnumerable<IPEndPoint> endpointList,
            IReadOnlyDictionary<string, string> txtRecord)
        {
            Name = name;
            HostName = hostName;
            IpEndpointList = endpointList.ToArray();
            TxtRecord = txtRecord;
        }
    }
}
