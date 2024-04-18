using System.Net;

namespace HomeKit.Net.Traffic
{
    public delegate void TrafficEventHandler(object sender, Packet packet);

    public class Packet
    {
        public DateTime UtcArrival;
        public IPEndPoint RemoteEndPoint;
        public byte[] Data;
    }
}