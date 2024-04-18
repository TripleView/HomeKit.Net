using System.Net.Sockets;

namespace HomeKit.Net.HttpServer;

public class ClientContext
{
    public string ConnectionString { get; set; }
    public TcpClient Client { get; set; }
    public HapHandler HapHandler { get; set; }
}