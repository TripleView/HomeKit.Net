namespace HomeKit.Net.HttpServer;

public delegate Task RequestDelegate(HttpContext context);

public class HttpServer
{
    public List<Channel> Channels { get; set; }
    public void AddChannel(Channel channel)
    {
        Channels.Add(channel);
    }
}