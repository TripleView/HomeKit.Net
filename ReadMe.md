# Homekit.Net存在的意义
通过本依赖包，用户可以通过代码模拟出各种各样的智能家居设备，并添加到苹果手机的家庭app中，这样我们就能在手机上控制这些模拟的智能家居设备执行一些我们在代码里配置好的操作，比如我们可以通过代码控制电脑打开或者关闭某个应用，然后利用本库封装为一个开关，那么我们就可以用家庭app中的这个模拟开关来控制应用了。有了原生api，大家就可以尽情的发挥想象力去搞事情了，比如DIY一个自动喂鱼机？
# Getting Started
## Nuget
 接下来我将演示如何使用【Homekit.Net】,你可以运行以下命令在你的项目中安装 Homekit.Net 。
 
 ```PM> Install-Package Homekit.Net ```
# 支持框架
net 6,net 8

# 示例

在demo示例中，当前已支持开关，传感器，空调。接下来演示如何使用：
通过继承类Accessory，我们就可以自定义一个自己的配件，在下面的示例中，我们定义一个开关，在构造函数中，我们加载一个名为Switch的服务，并且定义配件类型为开关，从switch服务中获取on这个特性，通过操作on这个特性，我们就可以通过代码模拟开关状态变化了，并且可以在苹果手机的家庭app上看到开关状态的变化。

````csharp
public class Switch : Accessory
{
    private bool IsOn;
    private Timer timer;
    public Characteristics CurrentOnCharacteristics { get; set; }

    public event Action<object> OnChange; 
    public Switch(AccessoryDriver accessoryDriver, string name, int? aid = null) : base(accessoryDriver, name, aid)
    {
        //加载switch开关服务
        var service = AddPreloadService("Switch");
        //定义配件种类为开关
        Category = Category.CATEGORY_SWITCH;
        //从switch服务中获取on这个特性
        CurrentOnCharacteristics = service.GetCharacteristics("On");
        //添加开关状态被家庭app改变后的回调函数
        CurrentOnCharacteristics.SetValueCallback = (o =>
        {
            OnChange(o);
            this.IsOn = (bool)o ;
        });
        //定义一个定时器，定时改变开关状态，用来模拟开关状态变化
        //timer = new Timer(Test, default, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public void Test(object? state)
    {
        var random = new Random();
        var number = random.Next(1, 50);
        var isOn = number % 2 == 0;
        CurrentOnCharacteristics.SetValue(isOn);
        timer.Change(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }
}
````

接下来，我们再来一个示例，定义一个温度传感器，在构造函数中，我们加载一个名为TemperatureSensor的服务，并且定义配件类型为传感器，从TemperatureSensor服务中获取CurrentTemperature(当前温度)这个特性，通过代码操作CurrentTemperature这个特性，我们就可以模拟温度变化，并且在苹果手机的家庭app上看到温度变化了。
````csharp
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
````

具体有哪些服务和特征，可以在程序运行起来后，查看Resources文件夹下的json文件
![服务与特性集合](https://img2024.cnblogs.com/blog/1323385/202404/1323385-20240419041043560-2052044370.png)

配件定义结束以后，我们就要让这个配件跑起来了，新建一个控制台程序，代码如下：
````csharp
 internal class Program
 {
     private async static Task SingleAccessory()
     {

         var cts = new CancellationTokenSource();
         //先定义驱动
         var driver = new AccessoryDriver(port: 6555);
         //定义配件
         var switchAccessory1 = new Switch(driver, "switch开关");
         //添加开关状态被苹果手机的家庭app改变后的回调
         switchAccessory1.OnChange += async (o) =>
         {
             Console.WriteLine("The switch state has changed.开关状态变化了");
         };

         driver.AddAccessory(switchAccessory1);
         await driver.StartAsync(cts.Token);
     }

     private async static Task MultipleAccessories()
     {

         var cts = new CancellationTokenSource();
         //先定义驱动
         var driver = new AccessoryDriver(port: 6554);
         //定义网关
         var bridge = new Bridge(driver, "网关");
         //定义配件1开关
         var switchAccessory1 = new Switch(driver, "开关switch");
         bridge.AddAccessory(switchAccessory1);
         //添加开关状态被苹果手机的家庭app改变后的回调
         switchAccessory1.OnChange += async (o) =>
         {
             Console.WriteLine("The switch state has changed.开关状态变化了");
         };
         //定义配件2传感器
         var temperatureSensor= new TemperatureSensor(driver, "传感器TemperatureSensor");
         bridge.AddAccessory(temperatureSensor);
         driver.AddAccessory(bridge);
         await driver.StartAsync(cts.Token);
     }

     async static Task Main(string[] args)
     {
         //Test Multiple Accessories 测试单配件
         await SingleAccessory();
         //Test Multiple Accessories 测试多配件
         //await MultipleAccessories();

     }
 }
````
以上这段代码分为2个部分，SingleAccessory单配件示例，和MultipleAccessories多配件示例，大体流程就是首先定义一个驱动，接着实例化之前定义的配件，并且把配件加入到驱动中，最后启动驱动即可。启动后效果如下图，他会在控制台上打印出一个二维码，

![启动效果](https://img2024.cnblogs.com/blog/1323385/202404/1323385-20240419043629890-1275148526.png)

接着我们使用苹果手机的家庭app扫描这个二维码，即可添加我们代码中自定义的配件。注意！苹果手机与程序必须处于同一个局域网，同时确保手机没有开启VPN。

![](https://img2024.cnblogs.com/blog/1323385/202404/1323385-20240419043915718-1949267028.png)

如果我们想在程序中定义多个配件，那么参考MultipleAccessories方法，首先得定义一个网关，接着把我们定义的多个配件添加到网关里，最后再启动驱动。

驱动支持持久化连接信息，信息存储在state.json中，这样应用重启后，也不需要重新扫码连接。

# 开源地址，欢迎star
本项目基于MIT协议开源，地址为
[https://github.com/TripleView/HomeKit.Net](https://github.com/TripleView/HomeKit.Net)

# 感谢以下项目
1. [HAP-Python](https://github.com/ikalchev/HAP-python)

2. [ZeroConfig](https://github.com/cosinekitty/zeroconfig)