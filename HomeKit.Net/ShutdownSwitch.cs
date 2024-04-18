using HomeKit.Net.Enums;

namespace HomeKit.Net;

public class ShutdownSwitch : Accessory
{
    public Characteristics CurrentTemperatureCharacteristics { get; set; }
    public ShutdownSwitch(AccessoryDriver accessoryDriver, string name, int? aid = null) : base(accessoryDriver, name, aid)
    {
        var service = AddPreloadService("Switch");
        Category = Category.CATEGORY_SWITCH;

        CurrentTemperatureCharacteristics = service.GetCharacteristics("On");
        // CurrentTemperatureCharacteristics.SetValue(1);
        // timer= new Timer(Test,null,TimeSpan.FromSeconds(30),TimeSpan.FromSeconds(30) );
    }
}