using System.Text;

namespace HomeKit.Net.HttpServer;

public static class HttpParser
{
    public static bool IsHtml(string body)
    {
        var rows = body.Split(Environment.NewLine);
        if (rows?.Length > 0)
        {
            var firstRow = rows[0];
            var firstRowArr = firstRow.Split(" ");
            if (firstRowArr?.Length == 3 && firstRowArr[2].Contains("HTTP"))
            {
                return true;
            }
        }

        return false;
    }

    public static HttpRequest Parse(byte[] bodyBytes)
    {
        var index = 0;
        var recBytes = bodyBytes;
        for (var i = 0; i < recBytes.Length; i++)
        {
            var by = recBytes[i];
            if (by == 13 && recBytes[i + 1] == 10 && recBytes[i + 2] == 13 && recBytes[i + 3] == 10)
            {
                index = i + 3;
                break;
            }
        }

        var body = Encoding.UTF8.GetString(bodyBytes);
        // Console.WriteLine("来了来了");
        if (!IsHtml(body))
        {
            throw new NotSupportedException("it is not html");
        }

        // Console.WriteLine("身体开始");
        // Console.WriteLine(body);
        // Console.WriteLine("身体结束");
        // if (Encoding.UTF8.GetBytes(body).Length > 300)
        // {
        //     Console.WriteLine(body);
        //     var a = 123;
        // }

        var bodyArr = bodyBytes[new Range(index + 1, bodyBytes.Length)];
        var request = new HttpRequest()
        {
            Body = new MemoryStream(bodyArr)
        };

        var rows = body.Split(Environment.NewLine);
        if (rows?.Length > 0)
        {
            var firstRow = rows[0];
            var firstRowArr = firstRow.Split(" ");
            if (firstRowArr?.Length == 3)
            {
                request.Path = firstRowArr[1];
                if (request.Path.Contains("?"))
                {
                    var queryArr = request.Path.Split("?")[1].Split("&");
                    foreach (var s in queryArr)
                    {
                        var queryKey = s.Split("=");
                        request.Query = new Dictionary<string, string>()
                        {
                            {queryKey[0], queryKey[1]}
                        };
                    }
                }
                switch (firstRowArr[0].ToLower())
                {
                    case "get":
                        request.HttpMethod = HttpMethod.Get;
                        break;
                    case "post":
                        request.HttpMethod = HttpMethod.Post;
                        break;
                    case "put":
                        request.HttpMethod = HttpMethod.Put;
                        break;
                    case "delete":
                        request.HttpMethod = HttpMethod.Delete;
                        break;
                    default:
                        throw new NotSupportedException($"not support httpMethod {firstRowArr[0]}");
                        break;
                }
                // (HttpMethod)Enum.Parse(typeof(HttpMethod), firstRowArr[0]);
            }

            foreach (var row in rows)
            {
                var rowArr = row.Split(":").ToList();
                if (rowArr.Count == 2 && rowArr[0].Trim() == "Host")
                {
                    request.Host = rowArr[1].Trim();
                }
            }
        }

        return request;
    }
}