using System.Diagnostics;
using System.Net;
using System.Timers;
using HomeKit.Net.Dns;
using HomeKit.Net.Dns;

namespace HomeKit.Net.Traffic
{
    public class Browser : IDisposable
    {
        private readonly TrafficMonitor monitor;
        private readonly Dictionary<string, ServiceCollection> serviceRoot = new();
        private readonly Dictionary<string, HostInfo> hostTable = new();
        private System.Timers.Timer expirationTimer;

        public static IDebugLogger Logger;

        public static void Log(string message)
        {
            if (Logger != null)
                Logger.Log(message);
        }

        public Browser(TrafficMonitor monitor)
        {
            this.monitor = monitor;
            monitor.OnReceive += OnPacket;

            expirationTimer = new System.Timers.Timer();
            expirationTimer.Interval = 1000.0;
            expirationTimer.Elapsed += OnExpirationTimer;
            expirationTimer.AutoReset = true;
            expirationTimer.Enabled = true;
        }

        private void OnExpirationTimer(object sender, ElapsedEventArgs e)
        {
            lock (serviceRoot)
            {
                foreach (var pair in serviceRoot)
                {
                    string serviceType = pair.Key;
                    ServiceCollection collection = pair.Value;

                    string[] obsoleteNames = collection.ServiceTable
                        .Where(kv => kv.Value.IsExpired())
                        .Select(kv => kv.Key)
                        .ToArray();

                    foreach (string name in obsoleteNames)
                    {
                        Browser.Log($"OnExpirationTimer: deleting [{name}] from [{serviceType}]");
                        collection.ServiceTable.Remove(name);
                    }

                    // FIXFIXFIX: should we expire TXT and SRV components too?
                }

                // Remove all expired IPv4 addresses from the host table.
                foreach (HostInfo host in hostTable.Values)
                    host.addressList.RemoveAll(addr => addr.IsExpired());

                // Delete the host itself if its IP address list becomes empty.
                string[] emptyHostNames = hostTable
                    .Where(kv => kv.Value.addressList.Count == 0)
                    .Select(kv => kv.Key)
                    .ToArray();

                foreach (string hostname in emptyHostNames)
                    hostTable.Remove(hostname);
            }
        }

        public void Dispose()
        {
            expirationTimer.Enabled = false;
            monitor.OnReceive -= OnPacket;
        }

        public string[] ServiceTypeList()
        {
            lock (serviceRoot)
                return serviceRoot.Keys.Select(st => RemoveLocalSuffix(st)).OrderBy(st => st).ToArray();
        }

        private const string LocalSuffix = ".local.";

        public static string RemoveLocalSuffix(string s)
        {
            if (s != null && s.EndsWith(LocalSuffix))
                return s.Substring(0, s.Length - LocalSuffix.Length);

            return s;
        }

        public static string AddLocalSuffix(string s)
        {
            if (s != null && !s.EndsWith(LocalSuffix))
                return s + LocalSuffix;

            return s;
        }

        public void RequestServiceTypes()
        {
            // Broadcast a request for all service types to be announced.
            // This probably only needs to be called one time at startup.
            RequestBrowse("_services._dns-sd._udp.local.");
        }

        public void RequestBrowse(string serviceType)
        {
            // Broadcast a request for all services of the given type to
            // announce themselves. This probably only needs to be called
            // once at startup.
            serviceType = AddLocalSuffix(serviceType);
            var message = new Message();
            message.Questions.Add(new Question(serviceType, QType.PTR, QClass.IN));
            monitor.Broadcast(message);
        }

        public ServiceBrowseResult[] Browse(string serviceType)
        {
            serviceType = AddLocalSuffix(serviceType);
            var list = new List<ServiceBrowseResult>();
            lock (serviceRoot)
            {
                if (serviceRoot.TryGetValue(serviceType, out ServiceCollection collection))
                {
                    foreach (var kv in collection.ServiceTable)
                    {
                        string name = kv.Key;
                        ServiceInfo info = kv.Value;
                        if (info.ptr != null)
                            list.Add(new ServiceBrowseResult(name, serviceType));
                    }
                }
            }
            return list.ToArray();
        }

        public ServiceResolveResult Resolve(ServiceBrowseResult browseResult, int resolveTimeoutInSeconds)
        {
            if (browseResult != null && !string.IsNullOrEmpty(browseResult.Name) && !string.IsNullOrEmpty(browseResult.ServiceType))
            {
                string name = browseResult.Name;
                string serviceType = AddLocalSuffix(browseResult.ServiceType);

                lock (serviceRoot)
                {
                    if (serviceRoot.TryGetValue(serviceType, out ServiceCollection collection))
                    {
                        if (collection.ServiceTable.TryGetValue(name, out ServiceInfo info))
                        {
                            if (info.srv != null && info.txt != null)
                            {
                                int port = (int)info.srv.Record.PORT;
                                string hostname = info.srv.Record.TARGET;
                                if (hostTable.TryGetValue(hostname, out HostInfo host))
                                {
                                    IPEndPoint[] endpointList = host.addressList
                                        .Select(fact => new IPEndPoint(fact.Record.Address, port))
                                        .ToArray();

                                    var txtRecord = new Dictionary<string, string>();
                                    foreach (string item in info.txt.Record.TXT)
                                    {
                                        string key;
                                        string value;
                                        int eq = item.IndexOf('=');
                                        if (eq < 0)
                                        {
                                            key = item;
                                            value = "";
                                        }
                                        else
                                        {
                                            key = item.Substring(0, eq);
                                            value = item.Substring(eq+1);
                                        }
                                        txtRecord[key] = value;
                                    }

                                    return new ServiceResolveResult(name, hostname, endpointList, txtRecord);
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        private static string FirstToken(string text)
        {
            // text = "iTunes_Ctrl_DF6D11C544851FEC._dacp._tcp.local."
            // return "iTunes_Ctrl_DF6D11C544851FEC"
            if (text != null)
            {
                int firstPeriodIndex = text.IndexOf('.');
                if (firstPeriodIndex > 0)
                    return text.Substring(0, firstPeriodIndex);
            }
            return null;
        }

        private static string RemainingText(string text)
        {
            // text = "iTunes_Ctrl_DF6D11C544851FEC._dacp._tcp.local."
            // return "_dacp._tcp.local."
            if (text != null)
            {
                int firstPeriodIndex = text.IndexOf('.');
                if (firstPeriodIndex > 0)
                    return text.Substring(firstPeriodIndex + 1);
            }
            return null;
        }

        private void OnPacket(object sender, Packet packet)
        {
            var message = new Message(packet.Data, true);

            foreach (Question question in message.Questions)
                Process(question);

            foreach (RR answer in message.Answers)
                Process(answer);

            foreach (RR answer in message.Authorities)
                Process(answer);

            foreach (RR answer in message.Additionals)
                Process(answer);
        }

        private void Process(Question question)
        {
            // do nothing (yet)
        }

        private void Process(RR answer)
        {
            if (answer.RECORD is RecordPTR ptr)
            {
                string serviceType = answer.NAME;
                string name = FirstToken(ptr.PTRDNAME);
                if (name != null && serviceType != null)
                {
                    Browser.Log($"Process: serviceType=[{serviceType}], PTRDNAME=[{ptr.PTRDNAME}]");
                    lock (serviceRoot)
                    {
                        if (serviceType == "_services._dns-sd._udp.local.")
                        {
                            LazyCreateServiceType(ptr.PTRDNAME);
                        }
                        else
                        {
                            ServiceCollection collection = LazyCreateServiceType(serviceType);
                            ServiceInfo info = collection.LazyCreate(name);
                            info.UpdatePtr(ptr);
                        }
                    }
                }
            }
            else if (answer.RECORD is RecordSRV srv)
            {
                // FIXFIXFIX: handle name conflicts discovered by existing "defenders" with the same name.
                string serviceType = RemainingText(answer.NAME);
                string name = FirstToken(answer.NAME);
                if (name != null && serviceType != null)
                {
                    lock (serviceRoot)
                    {
                        ServiceCollection collection = LazyCreateServiceType(serviceType);
                        ServiceInfo info = collection.LazyCreate(name);
                        info.UpdateSrv(srv);
                    }
                }
            }
            else if (answer.RECORD is RecordTXT txt)
            {
                string serviceType = RemainingText(answer.NAME);
                string name = FirstToken(answer.NAME);
                if (name != null && serviceType != null)
                {
                    lock (serviceRoot)
                    {
                        ServiceCollection collection = LazyCreateServiceType(serviceType);
                        ServiceInfo info = collection.LazyCreate(name);
                        info.UpdateTxt(txt);
                    }
                }
            }
            else if (answer.RECORD is RecordA ipv4)
            {
                string hostName = ipv4.RR.NAME;
                lock (serviceRoot)
                {
                    if (!hostTable.TryGetValue(hostName, out HostInfo hostInfo))
                        hostTable.Add(hostName, hostInfo = new HostInfo());

                    hostInfo.UpdateAddress(ipv4);
                }
            }
            else if (answer.RECORD is RecordAAAA ipv6)
            {
                // FIXFIXFIX: support IPv6 addresses too
            }
        }

        private ServiceCollection LazyCreateServiceType(string serviceType)
        {
            if (!serviceRoot.TryGetValue(serviceType, out ServiceCollection collection))
                serviceRoot.Add(serviceType, collection = new ServiceCollection());

            return collection;
        }
    }

    internal class ServiceCollection
    {
        public readonly Dictionary<string, ServiceInfo> ServiceTable = new();

        public ServiceInfo LazyCreate(string name)
        {
            if (!ServiceTable.TryGetValue(name, out ServiceInfo info))
                ServiceTable.Add(name, info = new ServiceInfo());

            return info;
        }
    }

    internal class ServiceInfo
    {
        public ServiceFact<RecordSRV> srv;
        public ServiceFact<RecordPTR> ptr;
        public ServiceFact<RecordTXT> txt;

        public bool IsExpired()
        {
            return (ptr != null) && (ptr.RemainingLifeInSeconds() < 0.0);
        }

        public void UpdateSrv(RecordSRV record)
        {
            if (srv == null || record.PRIORITY < srv.Record.PRIORITY)
            {
                srv = new ServiceFact<RecordSRV>(record);
                Browser.Log($"UpdateSrv: record = {record}");
            }
        }

        public void UpdatePtr(RecordPTR record)
        {
            ptr = new ServiceFact<RecordPTR>(record);
        }

        public void UpdateTxt(RecordTXT record)
        {
            txt = new ServiceFact<RecordTXT>(record);
        }
    }

    internal class ServiceFact<RecordType> where RecordType : Record
    {
        public Stopwatch Elapsed = Stopwatch.StartNew();
        public RecordType Record;

        public ServiceFact(RecordType record)
        {
            if (record == null)
                throw new ArgumentException("Null record not allowed", nameof(record));
            Record = record;
        }

        public double RemainingLifeInSeconds()
        {
            return (double)Record.RR.TTL - Elapsed.Elapsed.TotalSeconds;
        }

        public bool IsExpired()
        {
            return RemainingLifeInSeconds() < 0.0;
        }
    }

    internal class HostInfo
    {
        public readonly List<ServiceFact<RecordA>> addressList = new();

        public void UpdateAddress(RecordA record)
        {
            var fact = new ServiceFact<RecordA>(record);
            for (int i = 0; i < addressList.Count; ++i)
            {
                if (addressList[i].Record.Address.Equals(record.Address))
                {
                    addressList[i] = fact;
                    return;
                }
            }
            addressList.Add(fact);
        }
    }
}
