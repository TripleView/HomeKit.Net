using System.Net.Sockets;
using System.Text;

namespace HomeKit.Net.HttpServer;

public class HttpResponse
{
    public bool IsSend { get; private set; }

    /// <summary>Gets or sets the request protocol (e.g. HTTP/1.1).</summary>
    /// <returns>The request protocol.</returns>
    public string Protocol { get; set; } = "HTTP/1.1";

    public Dictionary<StatusCode, string> StatusCodeDescriptionDic = new Dictionary<StatusCode, string>()
    {
        {StatusCode.Status200OK, "OK"},
        {
            StatusCode.NO_CONTENT, "No Content"
        },
        {StatusCode.MULTI_STATUS, "Multi-Status"}
    };

    public StatusCode StatusCode { get; set; }

    public HapCrypto HapCrypto { get; set; }

    /// <summary>
    /// http headers
    /// </summary>
    public Dictionary<string, string> Headers { get; set; }

    /// <summary>
    /// http body
    /// </summary>
    private NetworkStream Body { get; set; }

    public List<byte> TempBytes { get; set; }

    public long? ContentLength { get; set; }

    /// <summary>Gets or sets the Content-Type header.</summary>
    /// <returns>The Content-Type header.</returns>
    public string? ContentType { get; set; }

    public HttpResponse(NetworkStream body)
    {
        TempBytes = new List<byte>();
        Headers = new Dictionary<string, string>();
        Body = body;
    }

    public HttpResponse Write(byte[] bytes)
    {
        TempBytes.AddRange(bytes);
        return this;
    }

    public void Send()
    {
        if (IsSend)
        {
            return;
        }
        else
        {
            IsSend = true;
        }

        Headers[HttpHeaders.ContentType] =
            string.IsNullOrWhiteSpace(ContentType) ? "text/plain; charset=utf-8" : ContentType;
        Headers[HttpHeaders.ContentLength] = TempBytes.Count.ToString();

        var header =
            $"{Protocol} {(int)StatusCode} {StatusCodeDescriptionDic[StatusCode]}{HttpCore.NewLine}";

        if (TempBytes.Count > 0)
        {
            // header += HttpCore.NewLine;
            foreach (var pair in Headers)
            {
                header += $"{pair.Key}: {pair.Value}{HttpCore.NewLine}";
            }
        }

        var headerBytes = header.ToBytes().ToList();
        // Console.WriteLine($"TempBytes.Count:{TempBytes.Count}");
        byte[] lineBytes = Encoding.UTF8.GetBytes(HttpCore.NewLine);
        headerBytes.AddRange(lineBytes);
        if (TempBytes.Count > 0)
        {
            headerBytes.AddRange(TempBytes);
        }


        var bytes = headerBytes.ToArray();

        var cc = bytes.GetString();
        // Console.WriteLine($"发送的报文为:{cc}");
        if (HapCrypto != null)
        {
            bytes = HapCrypto.Encrypt(bytes);
        }

        Body.Write(bytes, 0, bytes.Length);
        HapCrypto = null;
    }
}