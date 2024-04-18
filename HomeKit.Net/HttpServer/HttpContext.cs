namespace HomeKit.Net.HttpServer;

public class HttpContext
{
    public HttpRequest Request { get; set; }
    public HttpResponse Response { get; set; }
    public string ConnectionString { get; set; }
}