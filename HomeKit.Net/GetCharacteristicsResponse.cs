using HomeKit.Net.Enums;

namespace HomeKit.Net;

public class GetCharacteristicsResponse
{
    public List<GetCharacteristicsResponseItem> Characteristics { get; set; }
}

public class GetCharacteristicsResponseItem
{
    public int Aid { get; set; }
    public int Iid { get; set; }

    public HapServerStatus? Status { get; set; }
    public object? Value { get; set; }
}