using System.Text;
using HomeKit.Net.Enums;

namespace HomeKit.Net;

/// <summary>
/// 配件基类
/// </summary>
public class Accessory
{
    public Accessory(AccessoryDriver accessoryDriver, string name, int? aid = null)
    {
        AccessoryDriver = accessoryDriver;
        Name = name;
        Aid = aid ?? Const.STANDALONE_AID;
        Services = new List<Service>();
        IidManager = new IidManager();
        AddInfoService();
    }

    public Category Category { get; set; } = Category.CATEGORY_OTHER;

    /// <summary>
    /// Accessory Id
    /// 配件ID
    /// </summary>
    public int? Aid { get; set; }

    public AccessoryDriver AccessoryDriver { get; set; }

    /// <summary>
    /// Accessory Name
    /// 配件显示的名称
    /// </summary>
    public string Name { get; set; }

    public IidManager IidManager;
    /// <summary>
    /// Pair Verify One Encryption Context;配对验证阶段1的密钥上下文
    /// </summary>
    // public PairVerifyOneEncryptionContext PairVerifyOneEncryptionContext { get; set; }
    /// <summary>
    /// Accessory Services
    /// 配件拥有的服务
    /// </summary>
    public List<Service> Services { get; set; }

    /// <summary>
    /// add the required `AccessoryInformation` service
    /// 添加必须的配件信息服务
    /// </summary>
    public void AddInfoService()
    {
        var accessoryInformationService = Loader.LoadService("AccessoryInformation");
        AddService(accessoryInformationService);
        accessoryInformationService.ConfigureCharacteristics("Name", Name);
        accessoryInformationService.ConfigureCharacteristics("SerialNumber", "default");
    }

    /// <summary>
    /// Print setup message to console;在控制台打印设置设置信息
    /// </summary>
    public void SetupMessage()
    {
        var pinCode = Encoding.UTF8.GetString(AccessoryDriver.State.PinCode);
        var xhmUri = XhmUri();
        // xhmUri = "X-HM://00A05CLGYXGSL";
        Console.WriteLine($"Setup payload: {xhmUri}");
        Console.WriteLine("Scan this code with your HomeKit app on your iOS device:请使用家庭app扫描此二维码");
        QRConsole.Output(xhmUri);
        Console.WriteLine($"Or enter this code in your HomeKit app on your iOS device: {pinCode}");
    }

    /// <summary>
    /// A HAP representation of this Accessory;配件的hap表示
    /// </summary>
    public virtual AccessoryHapJson ToHap()
    {
        var result = new AccessoryHapJson()
        {
            Aid = Aid.Value,
            Services = new List<ServiceHapJson>()
        };
        foreach (var service in Services)
        {
            result.Services.Add(service.ToHap());
        }
        return result;
    }

    /// <summary>
    /// Add Service;添加服务
    /// </summary>
    /// <param name="service"></param>
    public void AddService(Service service)
    {
        service.Accessory = this;
        Services.Add(service);
        IidManager.Assign(service);
        foreach (var characteristics in service.CharacteristicsList)
        {
            characteristics.Accessory = this;
            IidManager.Assign(characteristics);
        }
    }

    /// <summary>
    /// get service；获取服务
    /// </summary>
    /// <param name="serviceName"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public Service GetService(string serviceName)
    {
        var service = Services.FirstOrDefault(it => it.Name == serviceName);
        if (service == null)
        {
            throw new Exception($"can not find service with name {serviceName}");
        }

        return service;
    }

    /// <summary>
    /// Generates the X-HM:// uri (Setup Code URI),生成配对时的url
    /// </summary>
    /// <returns></returns>
    public string XhmUri()
    {
        long payload = 0;
        payload |= 0 & 0x7;
        payload <<= 4;
        payload |= 0 & 0xF; // reserved bits

        payload <<= 8;
        payload |= (int)Category & 0xFF; // category

        payload <<= 4;
        payload |= 2 & 0xF; // flags
        payload <<= 27;

        var pinCode = Encoding.UTF8.GetString(AccessoryDriver.State.PinCode);
        // pinCode = "009-42-297";
        payload |= int.Parse(pinCode.Replace("-", "")) & 0x7FFFFFFF;

        var encodedPayload = Base36Converter.ConvertTo(payload);
        encodedPayload = encodedPayload.PadLeft(9, '0');
        // this.AccessoryDriver.State.SetupId = "76LR";
        return "X-HM://" + encodedPayload + AccessoryDriver.State.SetupId;
    }

    /// <summary>
    /// create a service with the given name and add it to this acc;根据给定名称创建一个服务，并添加到配件中
    /// </summary>
    /// <param name="serviceName"></param>
    public Service AddPreloadService(string serviceName)
    {
        var service = Loader.LoadService(serviceName);
        if (service == null)
        {
            throw new Exception($"can not find service with name {serviceName}");
        }

        AddService(service);
        return service;
    }

    public Characteristics GetAccessoryCharacteristic(int aid, int iid)
    {
        if (Aid != aid)
        {
            return null;
        }

        return (Characteristics)IidManager.GetObject(iid);
    }
    /// <summary>
    /// Set Primary Service；设置主服务
    /// </summary>
    public void SetPrimaryService(Service service)
    {
        foreach (var service1 in Services)
        {
            if (service.Iid == service1.Iid)
            {
                service1.IsPrimaryService = true;
                break;
            }
        }
    }

    public void Publish(object value, IAssignIid sender, string connectionString = "", bool immediate = false)
    {
        var sendData = new SendEventDataItem()
        {
            Aid = Aid.Value,
            Iid = IidManager.GetIid(sender),
            Value = value
        };
        AccessoryDriver.Publish(sendData, connectionString, immediate);
    }
}