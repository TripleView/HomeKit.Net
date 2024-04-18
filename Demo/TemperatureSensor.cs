using HomeKit.Net;
using HomeKit.Net.Enums;

namespace Demo;

public class TemperatureSensor : Accessory
{

    public Characteristics CurrentTemperatureCharacteristics { get; set; }

    private Timer timer;
    public TemperatureSensor(AccessoryDriver accessoryDriver, string name, CancellationToken token = default) : base(accessoryDriver, name)
    {
        //加载TemperatureSensor温度服务
        var service = AddPreloadService("TemperatureSensor");
        //定义配件种类为传感器
        Category = Category.CATEGORY_SENSOR;
        //从TemperatureSensor服务中获取CurrentTemperature(当前温度)这个特性
        CurrentTemperatureCharacteristics = service.GetCharacteristics("CurrentTemperature");
        //设置温度为1
        CurrentTemperatureCharacteristics.SetValue(1);
        //定义一个定时器，定时改变温度，用来模拟温度变化
        timer = new Timer(Test, token, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

    }

    public void Test(object? state)
    {
        if (state is CancellationToken token && token.IsCancellationRequested)
        {
            return;
        }
        // Console.WriteLine(DateTime.Now+"触发了定时任务");
        var random = new Random();
        var wd = random.Next(1, 50);
        // Console.WriteLine($"设置温度为{wd}度");
        CurrentTemperatureCharacteristics.SetValue(wd);
        timer.Change(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }
}