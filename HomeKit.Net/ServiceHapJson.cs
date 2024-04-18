namespace HomeKit.Net;

public class ServiceHapJson
{
    public int Iid { get; set; }

    public string Type { get; set; }

    public bool? Primary { get; set; }

    public List<CharacteristicsHapJson> Characteristics { get; set; }
}