using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace HomeKit.Net.HttpServer;

public class Channel : IDisposable
{
    public ChannelStatus Status { get; }
    public Guid Id { get; set; }

    private int numberOf = 0;

    private Dictionary<string, Dictionary<Regex, RequestDelegate>> PostMaps;
    private Dictionary<string, Dictionary<Regex, RequestDelegate>> GetMaps;
    private Dictionary<string, Dictionary<Regex, RequestDelegate>> PutMaps;
    private Dictionary<string, Dictionary<Regex, RequestDelegate>> DeleteMaps;
    public int Port { get; }

    // private bool isInitMap=false;
    // private Dictionary<string, HapHandler> HapHandlers;

    public Dictionary<string, ClientContext> ClientContexts { set; get; }
    public AccessoryDriver AccessoryDriver { get; set; }

    public Channel(int port, AccessoryDriver accessoryDriver)
    {
        Id = Guid.NewGuid();
        Status = ChannelStatus.Creating;
        Port = port;
        PostMaps = new Dictionary<string, Dictionary<Regex, RequestDelegate>>();
        GetMaps = new Dictionary<string, Dictionary<Regex, RequestDelegate>>();
        PutMaps = new Dictionary<string, Dictionary<Regex, RequestDelegate>>();
        DeleteMaps = new Dictionary<string, Dictionary<Regex, RequestDelegate>>();
        AccessoryDriver = accessoryDriver;
        // this.HapHandlers = new Dictionary<string, HapHandler>();
        ClientContexts = new Dictionary<string, ClientContext>();
    }


    private CancellationToken token;

    public async Task StartAsync(CancellationToken token = default)
    {
        var listener = new TcpListener(IPAddress.Any, Port); //开启了对端口的侦听
        this.token = token;
        listener.Start(); //开始侦听
        while (true)
        {
            if (token.IsCancellationRequested)
            {
                Console.WriteLine("我停止了");
                break;
            }
            //Console.WriteLine("我在运行");
            listener.BeginAcceptTcpClient(OnCompleteAcceptTcpClient, listener);
            await Task.Delay(100);
        }
    }

    private void OnCompleteAcceptTcpClient(IAsyncResult asyncResult)
    {
        try
        {
            Console.WriteLine("进来了");
            var listener = asyncResult.AsyncState as TcpListener;
            var tc = listener.EndAcceptTcpClient(asyncResult);
            DealTcp(tc);
            // Task.Run((() => DealTcp(tc)));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private void DealTcp(TcpClient tc)
    {
        Console.WriteLine($"连接的信息为{tc.Client.RemoteEndPoint.ToString()}");
        var allBytes = new List<byte>();
        var bytes = new byte[100];

        var data = new ChannelData()
        {
            Bytes = bytes,
            AllBytes = allBytes,
            TcpClient = tc,
            Count = 1
        };

        tc.GetStream().BeginRead(bytes, 0, bytes.Length, OnCompleteReadFromTCPClientStream, data);

        return;
    }

    private void InitMap(HapHandler hapHandler, string connectionString)
    {
        // if (isInitMap)
        // {
        //     return;
        // }
        //
        // isInitMap = true;

        MapDelegate(connectionString, PostMaps, "/pair-setup", hapHandler.PairSetup)
            .MapDelegate(connectionString, PostMaps, "/pair-verify", hapHandler.PairVerify)
            .MapDelegate(connectionString, PostMaps, "/pairings", hapHandler.Pairings)
            .MapDelegate(connectionString, GetMaps, "/accessories", hapHandler.GetAccessories)
            .MapDelegate(connectionString, GetMaps, "/characteristics", hapHandler.GetCharacteristics)
            .MapDelegate(connectionString, PutMaps, "/characteristics", hapHandler.SetCharacteristics);
    }


    /// <summary>
    /// Parse HttpRequest from body
    /// </summary>
    /// <param name="body"></param>
    /// <returns></returns>
    protected virtual HttpRequest ParseHttpRequest(byte[] body, string connectionString)
    {
        // Console.WriteLine($"ParseHttpRequest:{body.Length}");
        byte[] bodyClone = new byte[body.Length];
        Array.Copy(body, bodyClone, body.Length);

        if (ClientContexts.ContainsKey(connectionString))
        {
            var hapCrypto = ClientContexts[connectionString].HapHandler?.HapCrypto;
            if (hapCrypto != null)
            {
                hapCrypto.ReceiveData(body);
                body = hapCrypto.Decrypt();
                // Console.WriteLine($"ParseHttpRequest解析后长度:{body.Length}");
                if (body.Length == 0)
                {
                    body = bodyClone;
                }
            }
        }


        var httpRequest = HttpParser.Parse(body);
        return httpRequest;
    }


    protected virtual void DoResponse(HttpContext httpContext)
    {
        httpContext.Response.Write(Encoding.UTF8.GetBytes("1")).Send();
    }

    private async Task InternalDoResponse(HttpRequest httpRequest, NetworkStream socketStream, string connectionString)
    {
        var response = new HttpResponse(socketStream)
        {
            StatusCode = StatusCode.Status200OK
        };
        var httpContext = new HttpContext()
        {
            Request = httpRequest,
            Response = response,
            ConnectionString = connectionString
        };
        foreach (var keyValuePair in PostMaps[connectionString])
        {
            if (keyValuePair.Key.IsMatch(httpRequest.Path) && httpRequest.HttpMethod == HttpMethod.Post)
            {
                await keyValuePair.Value(httpContext);
                if (!response.IsSend)
                {
                    response.Send();
                }

                return;
            }
        }

        foreach (var keyValuePair in GetMaps[connectionString])
        {
            if (keyValuePair.Key.IsMatch(httpRequest.Path) && httpRequest.HttpMethod == HttpMethod.Get)
            {
                await keyValuePair.Value(httpContext);
                if (!response.IsSend)
                {
                    response.Send();
                }

                return;
            }
        }

        foreach (var keyValuePair in PutMaps[connectionString])
        {
            if (keyValuePair.Key.IsMatch(httpRequest.Path) && httpRequest.HttpMethod == HttpMethod.Put)
            {
                await keyValuePair.Value(httpContext);
                if (!response.IsSend)
                {
                    response.Send();
                }

                return;
            }
        }

        foreach (var keyValuePair in DeleteMaps[connectionString])
        {
            if (keyValuePair.Key.IsMatch(httpRequest.Path) && httpRequest.HttpMethod == HttpMethod.Delete)
            {
                await keyValuePair.Value(httpContext);
                if (!response.IsSend)
                {
                    response.Send();
                }

                return;
            }
        }

        DoResponse(httpContext);
    }

    private async void OnCompleteReadFromTCPClientStream(IAsyncResult result)
    {
        if (token.IsCancellationRequested)
        {
            return;
        }

        var data = result.AsyncState as ChannelData;
        var tc = data.TcpClient;
        var originConnectionString = tc.Client.RemoteEndPoint.ToString();
        var connectionString = originConnectionString.Split(":")[0];
        if (!tc.Connected)
        {
            Console.WriteLine($"{originConnectionString} Read connection is not Connected");
            // stream.Close();
            tc.Close();
            return;
        }
        var stream = tc.GetStream();
        var bytes = data.Bytes;
        var allBytes = data.AllBytes;

        var readCount = 0;
        try
        {
            readCount = stream.EndRead(result);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        if (readCount == 0)
        {
            Console.WriteLine($"{originConnectionString} Read connection dropped");
            // this.ClientContexts[connectionString].Client = tc;
            // this.ClientContexts[connectionString].ConnectionString = originConnectionString;
            // stream.Close();
            tc.Client.Disconnect(false);
            tc.Close();
            return;
        }

        // Console.WriteLine($"读取出的字节数为{readCount}--" + tc.Available);
        var tempByte = new byte[readCount];
        Array.Copy(bytes, 0, tempByte, 0, readCount);
        allBytes.AddRange(tempByte);
        Array.Clear(bytes);
        data.Count++;
        if (stream.Socket.Available == 0)
        {
            if (!ClientContexts.ContainsKey(connectionString))
            {
                var hapHandler = new HapHandler(AccessoryDriver);
                InitMap(hapHandler, connectionString);
                ClientContexts.Add(connectionString, new ClientContext()
                {
                    HapHandler = hapHandler,
                    ConnectionString = originConnectionString,
                    Client = tc
                });
            }
            else
            {
                if (ClientContexts[connectionString].ConnectionString != originConnectionString)
                {
                    var d = 123;
                    Console.WriteLine("同一个客户端，但是不一致了");
                    ClientContexts[connectionString].Client = tc;
                    ClientContexts[connectionString].ConnectionString = originConnectionString;
                }

                // ClientContexts[connectionString].HapHandler = hapHandler;
            }

            var body = Encoding.UTF8.GetString(allBytes.ToArray());
            var isHtml = HttpParser.IsHtml(body);
            // Console.WriteLine($"receive:{allBytes.Count}");
            // Console.WriteLine(Encoding.UTF8.GetString(allBytes.ToArray()));

            var httpRequest = ParseHttpRequest(allBytes.ToArray(), connectionString);
            Console.WriteLine($"http请求:{httpRequest.Path}");
            await InternalDoResponse(httpRequest, stream, connectionString);

            // Console.WriteLine($"处理完成，继续监听,当前网络状态：{stream.Socket.Connected}");
            data.Bytes = new byte[100];
            data.AllBytes.Clear();
            bytes = data.Bytes;

            stream.BeginRead(bytes, 0, bytes.Length, OnCompleteReadFromTCPClientStream, data);
            return;
        }

        stream.BeginRead(bytes, 0, bytes.Length, OnCompleteReadFromTCPClientStream, data);
    }

    public void Dispose()
    {
        // throw new NotImplementedException();
    }

    public Channel MapDelegate(
        string connectionString,
        Dictionary<string, Dictionary<Regex, RequestDelegate>> maps,
        string pattern,
        RequestDelegate requestDelegate)
    {
        var reg = new Regex("^" + pattern);
        if (maps.ContainsKey(connectionString))
        {
            if (!maps[connectionString].ContainsKey(reg))
            {
                maps[connectionString].Add(reg, requestDelegate);
            }
            else
            {
                maps[connectionString][reg] = requestDelegate;
            }
        }
        else
        {
            maps.Add(connectionString, new Dictionary<Regex, RequestDelegate>() { { reg, requestDelegate } });
        }

        return this;
    }
}

public class ChannelData
{
    public byte[] Bytes { get; set; }
    public List<byte> AllBytes { get; set; }
    public TcpClient TcpClient { get; set; }
    public int Count { get; set; }
}