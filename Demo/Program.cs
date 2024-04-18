using HomeKit.Net;

namespace Demo
{
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
}
