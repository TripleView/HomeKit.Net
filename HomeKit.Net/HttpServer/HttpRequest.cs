namespace HomeKit.Net.HttpServer;

public class HttpRequest
{
    public HttpMethod HttpMethod { get; set; }
    public string Path { get; set; }
    public Dictionary<string, string> Query { get; set; }
    public string Host { get; set; }
    public Stream Body { get; set; }
    public long? ContentLength { get; set; }

    /// <summary>Gets or sets the Content-Type header.</summary>
    /// <returns>The Content-Type header.</returns>
    public string? ContentType { get; set; }
}