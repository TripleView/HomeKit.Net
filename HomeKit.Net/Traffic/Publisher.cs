using System.Diagnostics;
using System.Net;
using System.Timers;
using Timer = System.Timers.Timer;
using HomeKit.Net.Dns;

namespace HomeKit.Net.Traffic
{
    public class Publisher : IDisposable
    {
        private readonly Dictionary<string, PublishContext> table = new Dictionary<string, PublishContext>();
        private readonly TrafficMonitor trafficMonitor;
        private bool closing;
        private readonly Timer timer = new Timer
        {
            Interval = 500.0,
            AutoReset = false,
            Enabled = true,
        };

        private const uint LongTimeToLive = 4500;
        private const uint ShortTimeToLive = 120;

        public Publisher(TrafficMonitor monitor)
        {
            trafficMonitor = monitor;
            timer.Elapsed += OnTimerTick;
            monitor.OnReceive += OnPacketReceived;
        }

        public void Dispose()
        {
            closing = true;     // prevent publishing anything new
            trafficMonitor.OnReceive -= OnPacketReceived;

            // Get a list of all published names.
            string[] allNames;
            lock (table)
                allNames = table.Keys.ToArray();

            // Begin the process of unpublishing every published item.
            foreach (string name in allNames)
                Unpublish(name);

            // Wait for background unpublish to complete and remove all the items.
            int count = 1;
            while (count != 0)
            {
                System.Threading.Thread.Sleep(100);
                lock (table)
                    count = table.Count;
            }

            timer.Stop();
            timer.Elapsed -= OnTimerTick;
            timer.Dispose();
        }

        private void OnTimerTick(object sender, ElapsedEventArgs e)
        {
            lock (table)
            {
                List<string> deletionList = null;

                foreach (PublishContext context in table.Values)
                {
                    switch (context.State)
                    {
                        case PublishState.Announce1:
                            if (--context.Countdown == 0)
                            {
                                trafficMonitor.Broadcast(context.AnnouncePacket);
                                context.State = PublishState.Announce2;
                                context.Countdown = 1;
                            }
                            break;

                        case PublishState.Announce2:
                            if (--context.Countdown == 0)
                            {
                                trafficMonitor.Broadcast(context.AnnouncePacket);
                                context.State = PublishState.Announce3;
                                context.Countdown = 4;
                            }
                            break;

                        case PublishState.Announce3:
                            if (--context.Countdown == 0)
                            {
                                trafficMonitor.Broadcast(context.AnnouncePacket);
                                context.State = PublishState.Ready;
                                context.Elapsed.Restart();
                                context.Countdown = 2;  // helps control TTL re-announcements
                            }
                            break;

                        case PublishState.Ready:
                            // If TTL is close to expiring, re-publish.
                            {
                                double targetSeconds;
                                switch (context.Countdown)
                                {
                                    case 2:
                                        targetSeconds = 0.5 * context.TimeToLiveSeconds;
                                        break;

                                    case 1:
                                        targetSeconds = 0.9 * context.TimeToLiveSeconds;
                                        break;

                                    case 0:
                                    default:
                                        targetSeconds = 0.95 * context.TimeToLiveSeconds;
                                        break;
                                }

                                double elapsedSeconds = context.Elapsed.Elapsed.TotalSeconds;
                                if (elapsedSeconds > targetSeconds)
                                {
                                    // Count down 2, 1, 0, then wrap back around to 2.
                                    context.Countdown = (2 + context.Countdown) % 3;

                                    // Every time we start over, we have to reset the clock.
                                    if (context.Countdown == 2)
                                        context.Elapsed.Restart();

                                    // Re-announce the published service on the network.
                                    trafficMonitor.Broadcast(context.AnnouncePacket);
                                }
                            }
                            break;

                        case PublishState.Unpublish2:
                            if (--context.Countdown == 0)
                            {
                                trafficMonitor.Broadcast(context.UnpublishPacket);
                                context.State = PublishState.Unpublish3;
                                context.Countdown = 2;
                            }
                            break;

                        case PublishState.Unpublish3:
                            if (--context.Countdown == 0)
                            {
                                trafficMonitor.Broadcast(context.UnpublishPacket);
                                // Put into separate deletion list so we don't mutate the dictionary while enumerating.
                                if (deletionList == null)
                                    deletionList = new List<string>();
                                deletionList.Add(context.Service.LongName);
                            }
                            break;
                    }
                }

                // Remove all completely-unpublished services.
                if (deletionList != null)
                    foreach (string longName in deletionList)
                        table.Remove(longName);
            }
            timer.Start();  // schedule next timer tick
        }

        private void OnPacketReceived(object sender, Packet packet)
        {
            var message = new Message(packet.Data, true);

            lock (table)
            {
                foreach (Question question in message.Questions)
                {
                    if (question.QType == QType.PTR)
                    {
                        // Respond to any relevant questions about things we have published.
                        foreach (PublishContext context in table.Values)
                        {
                            if (context.State == PublishState.Ready && context.Service.ServiceType == question.QName)
                            {
                                context.State = PublishState.Announce3;
                                context.Countdown = 1;
                                context.Elapsed.Restart();
                            }
                        }
                    }
                }
            }
        }

        public bool Publish(PublishedService service)
        {
            if (closing)
                return false;

            IPAddress serverIpAddress = TrafficMonitor.GetServerIPAddress();

            var context = new PublishContext
            {
                Service = service,
                State = PublishState.Announce1,
                Countdown = 2,
                AnnouncePacket = MakeAnnouncePacket(service, serverIpAddress),
                UnpublishPacket = MakeUnpublishPacket(service),
            };

            Message claim = MakeClaimPacket(context.Service, serverIpAddress);
            trafficMonitor.Broadcast(claim);

            lock (table)
            {
                table[service.LongName] = context;
            }

            return true;
        }

        public void Unpublish(string longName)
        {
            lock (table)
            {
                if (table.TryGetValue(longName, out PublishContext context))
                {
                    if (context.State != PublishState.Unpublish2 && context.State != PublishState.Unpublish3)
                    {
                        // Send first unpublish message.
                        trafficMonitor.Broadcast(context.UnpublishPacket);

                        // Set state for remaining 2 unpublish messages.
                        context.State = PublishState.Unpublish2;
                        context.Countdown = 2;
                    }
                }
            }
        }

        private static string LocalQualify(string shortName)
        {
            if (!shortName.EndsWith(".local."))
                return shortName + ".local.";
            return shortName;
        }

        public static Message MakeClaimPacket(PublishedService service, IPAddress serverIpAddress)
        {
            var message = new Message();

            string fqLongName = service.LongName + "." + service.ServiceType;     // "745E1C22FAFD@Living Room._raop._tcp.local."
            string localShortName = LocalQualify(service.ShortName);        // "Living-Room.local."

            // Question: name=[012345678@Walter White._fakeservice._tcp.local.] type=ANY class=IN
            message.Questions.Add(new Question(fqLongName, QType.ANY, QClass.IN));

            // Question: name=[heisenberg.local.] type=ANY class=IN
            message.Questions.Add(new Question(localShortName, QType.ANY, QClass.IN));

            // RR: name=[012345678@Walter White._fakeservice._tcp.local.] type=SRV class=IN TTL=120
            // 0 0 9456 heisenberg.local.
            var srv = new RecordSRV(0, 0, service.Port, localShortName);
            message.Authorities.Add(new RR(fqLongName, ShortTimeToLive, srv));

            // RR: name=[heisenberg.local.] type=A class=IN TTL=120
            // 192.168.1.3
            var arec = new RecordA(serverIpAddress.GetAddressBytes());
            message.Authorities.Add(new RR(localShortName, ShortTimeToLive, arec));

            return message;
        }

        public static Message MakeAnnouncePacket(PublishedService service, IPAddress serverIpAddress)
        {
            var message = new Message();
            message.header.QR = true;  // this is a response, not a question
            message.header.AA = true;  // this is an authoritative answer
            string fqLongName = service.LongName + "." + service.ServiceType;     // "745E1C22FAFD@Living Room._raop._tcp.local."
            string localShortName = LocalQualify(service.ShortName);        // "Living-Room.local."

            // ANSWER[0]:
            // RR: name=[745E1C2300FF@Office._raop._tcp.local.] type=SRV class=32769 TTL=120
            // 0 0 1024 Office.local.
            var srv = new RecordSRV(0, 0, service.Port, localShortName);
            message.Answers.Add(new RR(fqLongName, ShortTimeToLive, srv) { Class = (Class)0x8001 });

            // ANSWER[1]:
            // RR: name=[745E1C2300FF@Office._raop._tcp.local.] type=TXT class=32769 TTL=4500
            // TXT "txtvers=1"
            // TXT "ch=2"
            // ...
            var txt = new RecordTXT(service.TxtRecord);
            message.Answers.Add(new RR(fqLongName, LongTimeToLive, txt) { Class = (Class)0x8001 });

            // ANSWER[2]:
            // RR: name=[_services._dns-sd._udp.local.] type=PTR class=IN TTL=4500
            //_raop._tcp.local.
            var ptr1 = new RecordPTR(service.ServiceType);
            message.Answers.Add(new RR("_services._dns-sd._udp.local.", LongTimeToLive, ptr1));

            // ANSWER[3]:
            // RR: name=[_raop._tcp.local.] type=PTR class=IN TTL=4500
            // 745E1C2300FF@Office._raop._tcp.local.
            var ptr2 = new RecordPTR(fqLongName);
            message.Answers.Add(new RR(service.ServiceType, LongTimeToLive, ptr2));

            // ANSWER[4]:
            // RR: name=[Office.local.] type=A class=32769 TTL=120
            // 192.168.1.7
            var arec = new RecordA(serverIpAddress.GetAddressBytes());
            message.Answers.Add(new RR(localShortName, ShortTimeToLive, arec) { Class = (Class)0x8001 });

            // ANSWER[5]:
            // RR: name=[7.1.168.192.in-addr.arpa.] type=PTR class=32769 TTL=120
            // Office.local.
            string arpaIpName = ArpaIpName(serverIpAddress);    // "7.1.168.192.in-addr.arpa."
            var ptr3 = new RecordPTR(localShortName);
            message.Answers.Add(new RR(arpaIpName, ShortTimeToLive, ptr3) { Class = (Class)0x8001 });

            // ADDITIONAL[0]:
            // RR: name=[745E1C2300FF@Office._raop._tcp.local.] type=NSEC class=32769 TTL=120
            // NSEC 745E1C2300FF@Office._raop._tcp.local. [NSAPPTR, A6]
            var nsec1 = new RecordNSEC(fqLongName, HomeKit.Net.Dns.Type.NSAPPTR, HomeKit.Net.Dns.Type.A6);
            message.Additionals.Add(new RR(fqLongName, ShortTimeToLive, nsec1) { Class = (Class)0x8001 });

            // ADDITIONAL[1]:
            // RR: name=[Office.local.] type=NSEC class=32769 TTL=120
            // NSEC Office.local. [SOA]
            var nsec2 = new RecordNSEC(localShortName, HomeKit.Net.Dns.Type.SOA);
            message.Additionals.Add(new RR(localShortName, ShortTimeToLive, nsec2) { Class = (Class)0x8001 });

            return message;
        }

        public static Message MakeUnpublishPacket(PublishedService service)
        {
            string fqLongName = service.LongName + "." + service.ServiceType;     // "745E1C22FAFD@Living Room._raop._tcp.local."

            var message = new Message();
            message.header.QR = true;  // this is a response, not a question
            message.header.AA = true;  // this is an authoritative answer

            // FIXFIXFIX: With a little caution, we can create a single message for unpublishing multiple services.

            var ptr = new RecordPTR(fqLongName);
            message.Answers.Add(new RR(service.ServiceType, 0, ptr));

            return message;
        }

        private static string ArpaIpName(IPAddress serverIpAddress)
        {
            // 192.168.1.7 ==> "7.1.168.192.in-addr.arpa."
            string rev = string.Join(".", serverIpAddress.GetAddressBytes().Reverse().Select(b => b.ToString()));
            return rev + ".in-addr.arpa.";
        }
    }

    internal enum PublishState
    {
        Announce1,
        Announce2,
        Announce3,
        Ready,
        Unpublish2,
        Unpublish3,
    }

    internal class PublishContext
    {
        public PublishedService Service;
        public PublishState State;
        public int Countdown;
        public Message AnnouncePacket;
        public Message UnpublishPacket;
        public Stopwatch Elapsed = new Stopwatch();
        public double TimeToLiveSeconds = 4500.0;
    }
}
