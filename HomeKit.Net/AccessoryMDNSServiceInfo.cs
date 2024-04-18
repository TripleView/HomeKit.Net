namespace HomeKit.Net;

public class AccessoryMDNSServiceInfo
{
    public Accessory Accessory { get; set; }

    public State State { get; set; }


    public string ValidName { get; set; }
    public string ShortMac { get; set; }

    public string Name { get; set; }

    public string ValidHostName { get; set; }

    public string Server { get; set; }

    public AccessoryMDNSServiceInfo(Accessory accessory, State state)
    {
        Accessory = accessory;
        State = state;
        ValidName = GetValidName();
        ShortMac = state.Mac.Substring(9).Replace(":", "");
        Name = $"{ValidName}-{ShortMac}.";
        ValidHostName = GetValidHostName();
        Server = $"{ValidHostName}-{ShortMac}.local.";
    }

    /// <summary>
    /// Generate advertisement data from the accessory.;从配件生成广播数据
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, string> GetAdvertData()
    {
        var result = new Dictionary<string, string>()
        {
            {"md", GetValidName()},
            {"pv", Const.HAP_PROTOCOL_SHORT_VERSION},
            {"id", State.Mac},
            {"c#", State.ConfigVersion.ToString()},
            {"s#", "1"},
            {"ff", "0"},
            {"ci", ((int) Accessory.Category).ToString()},
            //'sf == 1' means "discoverable by HomeKit iOS clients"
            {"sf", State.IsPaired ? "0" : "1"},
            {"sh", SetupHash()}
        };
        return result;
    }

    private string SetupHash()
    {
        var str = State.SetupId + State.Mac;
        // str = "1JAB30:55:0B:13:FE:6F";
        var result = str.ToSha512ThenBase64();
        //State.SetupId + State.Mac;
        return result;
    }

    /// <summary>
    /// 待定
    /// </summary>
    /// <returns></returns>
    public string GetValidName()
    {
        return Accessory.Name;
    }

    public string GetValidHostName()
    {
        return Accessory.Name;
    }
}