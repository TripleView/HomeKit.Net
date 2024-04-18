using HomeKit.Net.Dns;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace HomeKit.Net.Traffic
{
    public class TrafficMonitor : IDisposable
    {
        private readonly object mutex = new object();
        private Thread queueWorkerThread;
        private readonly AutoResetEvent signal = new AutoResetEvent(false);
        private bool closed;
        private UdpClient[] clientList;
        private readonly Queue<Packet> inQueue = new();
        // FIXFIXFIX: add IPv6 client support.

        public event TrafficEventHandler OnReceive;

        private static bool IsWireless(IPAddress ip)
        {
            if (ip != null)
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    for (int i = 0; i < nic.GetIPProperties().UnicastAddresses.Count; i++)
                    {
                        if (nic.GetIPProperties().UnicastAddresses[i].Address.Equals(ip))
                        {
                            return (nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211);
                        }
                    }
                }
            }
            return false;
        }

        private static readonly object ipLock = new object();
        private static IPAddress stickyEthernetIpAddress;
        private static IPAddress stickyWifiIpAddress;

        public static IPAddress GetServerIPAddress()
        {
            lock (ipLock)
            {
                IPAddress[] IPAddresses = System.Net.Dns.GetHostAddresses("");
                if (IPAddresses != null)
                {
                    var ethernetList = new List<IPAddress>();
                    var wifiList = new List<IPAddress>();

                    foreach (IPAddress ip in IPAddresses)
                    {
                        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                        {
                            if (IsWireless(ip))
                                wifiList.Add(ip);
                            else
                                ethernetList.Add(ip);
                        }
                    }

                    // If there are any ethernet (wired) addresses, pick one.
                    // Any ethernet address is preferred over any wifi address.
                    if (ethernetList.Count > 0)
                    {
                        // If we have already started using a particular ethernet address,
                        // make it sticky and keep using it, if that address is still available.
                        // We do this just in case the order of the entries in IPAddresses changes.
                        if (stickyEthernetIpAddress != null)
                            foreach (IPAddress ip in ethernetList)
                                if (ip.Equals(stickyEthernetIpAddress))
                                    return ip;

                        // We could not re-use the sticky ethernet address.
                        // Pick the first one we find, and switch to it as a new sticky address.
                        stickyEthernetIpAddress = ethernetList[0];
                        return stickyEthernetIpAddress;
                    }

                    // We could not find any wired (ethernet) addresses to use.
                    // Fall back to a wireless (wifi) address.
                    if (wifiList.Count > 0)
                    {
                        if (stickyWifiIpAddress != null)
                            foreach (IPAddress ip in wifiList)
                                if (ip.Equals(stickyWifiIpAddress))
                                    return ip;

                        // We could not re-use the sticky ethernet address.
                        // Pick the first one we find, and switch to it as a new sticky address.
                        stickyWifiIpAddress = wifiList[0];
                        return stickyWifiIpAddress;
                    }
                }

                return null;
            }
        }

        public void Start()
        {
            lock (mutex)
            {
                if (closed)
                    throw new Exception("Cannot restart a TrafficMonitor after it has been disposed.");

                if (clientList != null)
                    throw new Exception("TrafficMonitor has already been started.");

                queueWorkerThread = new Thread(QueueWorkerThread)
                {
                    IsBackground = true,
                    Name = "ZeroConfig TrafficMonitor queue worker",
                };
                queueWorkerThread.Start();

                // FIXFIXFIX: adapt to changing network conditions (adapters being added/removed, IP address changes, etc.)
                clientList = MakeClientList();
                foreach (UdpClient client in clientList)
                    client.BeginReceive(ReceiveCallback, client);
            }
        }

        public int ListeningAdapterCount
        {
            get
            {
                lock (mutex)
                {
                    return (clientList == null) ? 0 : clientList.Length;
                }
            }
        }

        public void Broadcast(byte[] datagram)
        {
            var broadcastEndpoint = new IPEndPoint(IPAddress.Parse("224.0.0.251"), 5353);
            lock (mutex)
            {
                foreach (UdpClient client in clientList)
                    client.BeginSend(datagram, datagram.Length, broadcastEndpoint, null, null);
            }
        }

        public void Broadcast(Message message)
        {
            var writer = new RecordWriter();
            message.Write(writer);
            byte[] datagram = writer.GetData();
            Broadcast(datagram);
        }

        private static UdpClient[] MakeClientList()
        {
            NetworkInterface[] adapterList = GetMulticastAdapterList();
            var clientList = new List<UdpClient>();
            foreach (NetworkInterface adapter in adapterList)
            {
                int adapterIndex = adapter.GetIPProperties().GetIPv4Properties().Index;
                UdpClient client = new UdpClient();
                Socket socket = client.Client;
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, IPAddress.HostToNetworkOrder(adapterIndex));
                client.ExclusiveAddressUse = false;
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                //socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 2000);
                //client.ExclusiveAddressUse = false;
                var localEp = new IPEndPoint(IPAddress.Any, 5353);
                socket.Bind(localEp);
                var multicastAddress = IPAddress.Parse("224.0.0.251");
                var multOpt = new MulticastOption(multicastAddress, adapterIndex);
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, multOpt);
                clientList.Add(client);
            }

            if (clientList.Count == 0)
                throw new Exception("Could not find any multicast network adapters.");

            return clientList.ToArray();
        }

        private static NetworkInterface[] GetMulticastAdapterList()
        {
            // https://stackoverflow.com/questions/2192548/specifying-what-network-interface-an-udp-multicast-should-go-to-in-net
            // https://windowsasusual.blogspot.com/2013/01/socket-option-multicast-interface.html

            var list = new List<NetworkInterface>();
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in nics)
            {
                if (!adapter.SupportsMulticast)
                    continue; // multicast is meaningless for this type of connection

                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                if (OperationalStatus.Up != adapter.OperationalStatus)
                    continue; // this adapter is off or not connected

                IPv4InterfaceProperties p = adapter.GetIPProperties().GetIPv4Properties();
                if (null == p)
                    continue; // IPv4 is not configured on this adapter

                int index;
                try
                {
                    index = p.Index;
                }
                catch (Exception)
                {
                    continue;   // skip adapters without indexes
                }
                list.Add(adapter);
            }
            return list.ToArray();
        }

        public void Dispose()
        {
            lock (mutex)
            {
                if (closed)
                    return;     // ignore redundant calls

                closed = true;
                if (clientList != null)
                {
                    foreach (UdpClient client in clientList)
                        client.Dispose();

                    clientList = null;
                }
            }
            signal.Set();   // wake up worker thread so it notices we are closing; it then exits immediately.
            queueWorkerThread.Join();
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            lock (mutex)
            {
                if (closed)
                    return;

                UdpClient client = (UdpClient)result.AsyncState;
                IPEndPoint remoteEndPoint = null;
                byte[] data = client.EndReceive(result, ref remoteEndPoint);
                var packet = new Packet { Data = data, RemoteEndPoint = remoteEndPoint, UtcArrival = DateTime.UtcNow };
                inQueue.Enqueue(packet);
                client.BeginReceive(ReceiveCallback, client);
            }
            signal.Set();
        }

        private void QueueWorkerThread()
        {
            while (true)
            {
                signal.WaitOne();
                while (true)
                {
                    Packet packet;
                    lock (mutex)
                    {
                        if (closed)
                            return;

                        if (inQueue.Count == 0)
                            break;

                        packet = inQueue.Dequeue();
                    }

                    if (OnReceive != null)
                        OnReceive(this, packet);
                }
            }
        }
    }
}
