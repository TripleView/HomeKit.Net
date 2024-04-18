using HomeKit.Net;
using HomeKit.Net.Enums;
using static System.Net.Mime.MediaTypeNames;
using ZXing.Aztec.Internal;

namespace Demo;

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