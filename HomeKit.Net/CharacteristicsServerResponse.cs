namespace HomeKit.Net;

public class CharacteristicsServerResponse
{
    public List<CharacteristicsServerResponseItem> Characteristics { get; set; }
}

public class CharacteristicsServerResponseItem
{
    public int Aid { get; set; }
    public int Iid { get; set; }
    /// <summary>
    /// Value to set;要设置的值
    /// </summary>
    public object? Value { get; set; }
    /// <summary>
    /// (Un)subscribe for events from this characteristics.订阅来自这个特征的事件。
    /// </summary>
    public bool? Ev { get; set; }
}