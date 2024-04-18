namespace HomeKit.Net;

public class SendEventData
{
    public List<SendEventDataItem> Characteristics { get; set; }
}

public class SendEventDataItem
{
    public int Aid { get; set; }
    public int Iid { get; set; }
    public object Value { get; set; }
}
