using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using HomeKit.Net;
using HomeKit.Net.HttpServer;
using HomeKit.Net.Serialize;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ZXing;
using PublishedService = HomeKit.Net.Traffic.PublishedService;
using Publisher = HomeKit.Net.Traffic.Publisher;
using TrafficMonitor = HomeKit.Net.Traffic.TrafficMonitor;

// using ServiceDiscovery = HomeKitSharp.HomeKit.MDns.ServiceDiscovery;
// using ServiceProfile = HomeKitSharp.HomeKit.MDns.ServiceProfile;

namespace HomeKit.Net;

public class AccessoryDriver
{
    public IPAddress Address { get; set; }

    public Accessory Accessory { get; set; }

    public State State { get; set; }
    public Dictionary<string, List<string>> Topics { get; set; }

    public SrpServer SrpServer { set; get; }
    public AccessoryMDNSServiceInfo MdnsServiceInfo { get; set; }

    public int Port { get; set; }

    public Channel Channel { get; set; }

    private object lockObj = new object();

    private CancellationToken token;

    private bool isFinalClose = false;

    private string basePath = Path.Combine(AppContext.BaseDirectory, "state.json");
    /// <summary>
    /// Default log level|默认的日志等级
    /// </summary>
    public LogLevel DefaultLogLevel { get; set; }

    private ILogger<AccessoryDriver> logger;
    public AccessoryDriver(byte[] pinCode = null, int port = 51234, string mac = null, LogLevel defaultLogLevel = LogLevel.Information)
    {
        Address = Utils.GetIpAddress();
        Port = port;
        Topics = new Dictionary<string, List<string>>();
        State = new State(address: Address, pinCode: pinCode, mac: mac, port: port);

        DefaultLogLevel = defaultLogLevel;

        logger = LoggerFactory.Create(it =>
        {
            it.AddFilter(level => level == DefaultLogLevel);
            it.AddConsole();
        }).CreateLogger<AccessoryDriver>();

        var hasOldState = RestoreState();
        if (!hasOldState)
        {
            State = new State(address: Address, pinCode: pinCode, mac: mac, port: port);
            PersistentState();
        }

        AppDomain.CurrentDomain.UnhandledException += this.UnhandledException;
        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        //new Timer((a)=>
        //{
        //    PersistentState(true);
        //}, null, TimeSpan.FromSeconds(60), TimeSpan.FromHours(1));
    }

    /// <summary>
    /// 还原state
    /// </summary>
    /// <returns></returns>
    private bool RestoreState()
    {
        var fi = new FileInfo(basePath);
        var hasOldState = false;//判断是否有旧的状态
        if (fi.Exists)
        {
            using (var fs = fi.OpenRead())
            {
                using (var sw = new StreamReader(fs))
                {
                    var stateTxt = sw.ReadToEnd();
                    var settings = new JsonSerializerSettings();
                    settings.Converters.Add(new IPAddressConverter());
                    settings.Converters.Add(new IPEndPointConverter());
                    settings.Formatting = Formatting.Indented;
                    var oldState = JsonConvert.DeserializeObject<State>(stateTxt, settings);
                    if (oldState.Address.Equals(Address) && this.Port == oldState.Port)
                    {
                        hasOldState = true;
                        this.State = oldState;
                    }
                }
            }


        }

        return hasOldState;
    }

    private void CurrentDomain_ProcessExit(object? sender, EventArgs e)
    {
        PersistentState();
    }

    private void UnhandledException(object e, UnhandledExceptionEventArgs ex)
    {
        PersistentState();
    }

    /// <summary>
    /// 持久化状态
    /// </summary>
    private void PersistentState(bool isTimerTrigger = false)
    {

        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new IPAddressConverter());
        settings.Converters.Add(new IPEndPointConverter());
        settings.Formatting = Formatting.Indented;

        var c = this.State.IsPaired;
        var state = JsonConvert.SerializeObject(this.State, settings);
        var fi = new FileInfo(basePath);
        if (fi.Exists)
        {
            fi.Delete();
            Thread.Sleep(100);
        }

        using (var fs = File.OpenWrite(basePath))
        {
            using (var sr = new StreamWriter(fs))
            {
                sr.Write(state);
                sr.Flush();
                sr.Close();
            }
        }
    }

    /// <summary>
    /// Called when a client has paired with the accessory.Persist the new accessory state;当客户端与配件配对时调用。保持新的配件状态
    /// </summary>
    /// <param name="clientUuid"></param>
    /// <param name="clientPublicKey"></param>
    /// <param name="perms"></param>
    public bool Pair(Guid clientUuid, byte[] clientPublicKey, byte[] perms, string connectionString)
    {
        logger.LogDebug($"{connectionString} Paired with {clientUuid} with permissions {perms.GetString()}");
        State.AddPairedClient(clientUuid, clientPublicKey, perms);
        PersistentState();
        return true;
        // async_persist
    }

    /// <summary>
    /// Removes the paired client from the accessory；解除配对
    /// </summary>
    /// <param name="clientUuid"></param>
    /// <returns></returns>
    public bool UnPair(Guid clientUuid)
    {
        State.RemovePairedClient(clientUuid);
        PersistentState();
        return true;
    }

    public async Task StartAsync(CancellationToken token = default)
    {
        if (Accessory == null)
        {
            throw new Exception("You must assign an accessory to the driver, before you can start it.");
        }

        this.token = token;
        //注册回调
        token.Register(() =>
        {
            var clientContext = Channel?.ClientContexts?.Select(it => it.Value).ToList();
            if (clientContext != null)
            {
                foreach (var context in clientContext)
                {
                    try
                    {
                        if (context.Client.Connected)
                        {
                            context.Client.Dispose();
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogError("error",e);
                    }
                }

            }
        });
        //Print accessory setup message
        if (!State.IsPaired)
        {
            Accessory.SetupMessage();
        }

        MdnsServiceInfo = new AccessoryMDNSServiceInfo(Accessory, State);

        //  var sd = new ServiceDiscovery();
        // //发布一个服务，服务名称是有讲究的，一般都是_开头的，可以找一下相关资料
        //  var p = new ServiceProfile(MdnsServiceInfo.Name, Const.HAP_SERVICE_TYPE, 5010,
        //      new List<IPAddress>() {Utils.GetIpAddress()});
        //
        //  var dicPropertys = MdnsServiceInfo.GetAdvertData();
        //  foreach (var pair in dicPropertys)
        //  {
        //      p.AddProperty(pair.Key, pair.Value);
        //  }
        //
        //  sd.Mdns.UseIpv6 = false;
        //
        //  sd.Advertise(p);
        var dicPropertys = MdnsServiceInfo.GetAdvertData();

        var service = new HomeKitService();
        Console.WriteLine("mac address:" + State.Mac.Substring(8));
        using (var monitor = new TrafficMonitor())
        {
            monitor.Start();
            using (var publisher = new Publisher(monitor))
            {
                var pub = new PublishedService
                {
                    Client = service,
                    LongName = MdnsServiceInfo.Name,
                    ShortName = MdnsServiceInfo.Name,
                    ServiceType = Const.HAP_SERVICE_TYPE + "local.",
                    Port = (ushort)this.Port,
                    TxtRecord = dicPropertys,
                };
                // MdnsServiceInfo.Name+Const.HAP_SERVICE_TYPE+"local."5011
                if (publisher.Publish(pub))
                {
                    Console.WriteLine("Publish succeeded. Press ENTER to unpublish and exit.");
                    Channel = new Channel(this.Port, this);
                    await Channel.StartAsync(token);
                    // Console.ReadLine();
                }
                else
                {
                    Console.WriteLine("ERROR: Publish failed.");
                }
            }
        }

        // return sd;
    }

    /// <summary>
    /// add top level accessory to driver;向驱动添加顶级配件
    /// </summary>
    /// <param name="accessory"></param>
    public void AddAccessory(Accessory accessory)
    {
        Accessory = accessory;
    }

    /// <summary>
    /// Create an SRP verifier for the accessory's info.为配件信息创建一个 SRP 验证器。
    /// </summary>
    public void SetupSrpVerifier()
    {
        var identityBytes = Encoding.UTF8.GetBytes("Pair-Setup");
        SrpServer = new SrpServer(SHA512.Create().ComputeHash, identityBytes, State.PinCode);
    }

    /// <summary>
    /// Subscribe the given client from the given topic;从给定主题订阅给定客户端
    /// </summary>
    public async Task SubscribeClientTopic(string connectionString, string topic, bool subscribe)
    {
        connectionString = connectionString.Split(":")[0];
        if (subscribe)
        {
            List<string> clientInfos;
            if (Topics.ContainsKey(topic))
            {
                lock (lockObj)
                {
                    clientInfos = Topics[topic];
                    if (!clientInfos.Contains(connectionString))
                    {
                        clientInfos.Add(connectionString);
                    }
                }
            }
            else
            {
                clientInfos = new List<string>()
                {
                    connectionString
                };
                lock (lockObj)
                {
                    Topics.Add(topic, clientInfos);
                }
            }
        }
        else
        {
            lock (lockObj)
            {
                if (Topics.Keys.All(it => it != topic))
                {
                    return;
                }

                // this.Topics[topic].
                Topics[topic].Remove(connectionString);
            }
        }
    }

    public AccessorysHapJson GetAccessories()
    {

        var result = new AccessorysHapJson();

        if (Accessory is Bridge bridge)
        {
            result.Accessories = bridge.ToHap();
        }
        else
        {
            result.Accessories = new List<AccessoryHapJson>() { Accessory.ToHap() };
        }
        return result;
    }

    public void Publish(SendEventDataItem sendDataItem, string connectionString = "", bool immediate = false)
    {
        var topic = Const.GetTopic(sendDataItem.Aid, sendDataItem.Iid);
        if (!Topics.ContainsKey(topic))
        {
            return;
        }

        var subscribeClients = Topics[topic];

        var jsonSetting = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        var sendData = new SendEventData()
        {
            Characteristics = new List<SendEventDataItem>() { sendDataItem }
        };

        var currentJson = JsonConvert.SerializeObject(sendData, jsonSetting);
        var bytes = currentJson.ToBytes();
        var sendDataBytes =
            $"EVENT/1.0 200 OK\r\nContent-Type: application/hap+json\r\nContent-Length: {bytes.Length}\r\n\r\n{currentJson}"
                .ToBytes();

        lock (lockObj)
        {
            var removeList = new List<string>();
            foreach (var subscribeClient in subscribeClients)
            {
                //Skip sending event to client since its the client that made the characteristic change
                if (!string.IsNullOrWhiteSpace(connectionString) && connectionString == subscribeClient)
                {
                    continue;
                }

                var clientContext = Channel.ClientContexts.Where(it => it.Key == subscribeClient
                ).Select(it => it.Value).FirstOrDefault();

                if (clientContext == null || clientContext.Client == null || clientContext.Client.Client == null)
                {
                    continue;
                }

                if (!clientContext.Client.Connected && !clientContext.Client.Client.Connected)
                {
                    continue;
                }

                var hapCrypto = clientContext.HapHandler.HapCrypto;
                if (hapCrypto != null)
                {
                    sendDataBytes = hapCrypto.Encrypt(sendDataBytes);
                }

                try
                {
                    var tc = clientContext.Client.GetStream();
                    tc.Write(sendDataBytes, 0, sendDataBytes.Length);
                    // logger.LogDebug(subscribeClient + "发送成功");
                }
                catch (Exception e)
                {
                    logger.LogError("Publish error", e);
                    // throw;
                }
            }

            if (removeList.Any())
            {
                // subscribeClients.RemoveAll(it => removeList.Contains(it));
            }
        }
    }
}