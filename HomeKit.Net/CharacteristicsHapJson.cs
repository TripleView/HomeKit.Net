using Newtonsoft.Json;

namespace HomeKit.Net;

public class CharacteristicsHapJson
{
    public int Iid { get; set; }

    public string Type { get; set; }

    public List<string> Perms { get; set; }

    public string Format { get; set; }

    public int? MaxLen { get; set; }

    public double? MaxValue { get; set; }

    public double? MinStep { get; set; }

    public double? MinValue { get; set; }

    public string Unit { get; set; }

    [JsonProperty("valid-values")]
    public List<decimal> ValidValues { get; set; }
    public object? Value { get; set; }
}