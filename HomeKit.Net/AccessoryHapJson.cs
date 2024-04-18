namespace HomeKit.Net;

public class AccessoryHapJson
{
    public int Aid { get; set; }

    public List<ServiceHapJson> Services { get; set; }
}

public class AccessorysHapJson
{
    public List<AccessoryHapJson> Accessories { get; set; }
}