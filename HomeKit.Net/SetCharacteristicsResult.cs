using HomeKit.Net.Enums;

namespace HomeKit.Net;

public class SetCharacteristicsResult
{
    public List<SetCharacteristicsResultItem> Characteristics { get; set; }
}

public class SetCharacteristicsResultItem
{
    public int Aid { get; set; }
    public int Iid { get; set; }
    public HapServerStatus Status { get; set; }
}
