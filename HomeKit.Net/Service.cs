namespace HomeKit.Net;

/// <summary>
/// 配件的服务类
/// </summary>
public class Service : IAssignIid
{
    /// <summary>
    /// Service Id
    /// 服务ID
    /// </summary>
    public Guid Iid { get; set; }

    /// <summary>
    /// Service Name
    /// 服务名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Service CharacteristicsList
    /// 服务的特征列表
    /// </summary>
    public List<Characteristics> CharacteristicsList { get; set; }

    public Accessory Accessory { get; set; }

    public string HapType { get; set; }
    /// <summary>
    ///Is Primary Service； 是否为主服务
    /// </summary>
    public bool? IsPrimaryService { get; set; }

    public Service(Guid iid, string name)
    {
        Iid = iid;
        Name = name;
        CharacteristicsList = new List<Characteristics>();
        SetHapType();
    }

    public Service(Guid iid, string name, List<Characteristics> characteristicsList)
    {
        Iid = iid;
        Name = name;
        CharacteristicsList = characteristicsList;
        SetHapType();
    }

    public void SetHapType()
    {
        HapType = Utils.GuidToHapType(Iid);
    }
    /// <summary>
    /// Add Characteristic
    /// 为服务添加特征列表
    /// </summary>
    public void AddCharacteristic(List<Characteristics> characteristicsList)
    {
        var addCharacteristicsList =
            characteristicsList.Where(it => !CharacteristicsList.Exists(x => x.Iid == it.Iid)).ToList();
        foreach (var characteristics in addCharacteristicsList)
        {
            CharacteristicsList.Add(characteristics);
        }
    }

    public void ConfigureCharacteristics(string characteristicsName, object value, Action<object> setterCallback = null, Func<object> getterCallback = null)
    {
        var characteristics = CharacteristicsList.FirstOrDefault(it => it.Name == characteristicsName);
        if (characteristics == null)
        {
            throw new Exception($"Characteristics:{characteristicsName} not exist");
        }

        if (setterCallback != null)
        {
            characteristics.SetValueCallback = setterCallback;
        }

        if (getterCallback != null)
        {
            characteristics.GetValueCallback = getterCallback;
        }

        if (value != null)
        {
            characteristics.SetValue(value);
        }

    }

    /// <summary>
    /// Get Characteristics by name;通过名称获取特征
    /// </summary>
    /// <param name="characteristicsName"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public Characteristics GetCharacteristics(string characteristicsName)
    {
        var characteristics = CharacteristicsList.FirstOrDefault(it => it.Name == characteristicsName);
        if (characteristics == null)
        {
            throw new Exception($"Characteristics:{characteristicsName} not exist");
        }

        return characteristics;
    }

    public ServiceHapJson ToHap()
    {
        var result = new ServiceHapJson()
        {
            Iid = Accessory.IidManager.GetIid(this),
            Primary = IsPrimaryService,
            Type = HapType,
            Characteristics = new List<CharacteristicsHapJson>()
        };

        foreach (var characteristics in CharacteristicsList)
        {
            result.Characteristics.Add(characteristics.ToHap());
        }

        return result;
    }

}