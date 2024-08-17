using System.Diagnostics;
using HomeKit.Net;
using HomeKit.Net.Enums;
using static System.Net.Mime.MediaTypeNames;
using ZXing.Aztec.Internal;

namespace Demo;

public enum HeatingCoolingState
{
    /// <summary>
    /// 关闭
    /// </summary>
    Close = 0,
    /// <summary>
    /// 加热
    /// </summary>
    Heat = 1,
    /// <summary>
    /// 制冷
    /// </summary>
    Cool = 2,
    /// <summary>
    /// 自动
    /// </summary>
    Auto = 3
}

/// <summary>
/// Temperature Display Units |温度显示单位
/// </summary>
public enum TemperatureDisplayUnits
{
    /// <summary>
    /// 摄氏度
    /// </summary>
    Celsius = 0,
    /// <summary>
    /// 华氏摄氏度
    /// </summary>
    Fahrenheit = 1
}

public class AirConditioner : Accessory
{
    private Timer timer;

    /// <summary>
    /// Current Temperature|当前温度
    /// </summary>
    public float CurrentTemperature { set; get; }
    /// <summary>
    /// Current temperature characteristics|当前温度特性
    /// </summary>
    public Characteristics CurrentTemperatureCharacteristics { get; set; }
    /// <summary>
    ///Current temperature change callback function|当前温度改变回调函数
    /// </summary>
    public event Action<float> CurrentTemperatureSetValueCallBack;


    /// <summary>
    /// Target temperature characteristics|目标温度特性
    /// </summary>
    public Characteristics TargetTemperatureCharacteristics { get; set; }
    /// <summary>
    /// Target temperature|目标温度
    /// </summary>
    public float TargetTemperature { set; get; }

    /// <summary>
    /// Target temperature change callback function|目标温度改变回调函数
    /// </summary>
    public event Action<float> TargetTemperatureSetValueCallBack;


    /// <summary>
    /// Current Heating Cooling State Characteristics|当前制冷制热状态特性
    /// </summary>
    public Characteristics CurrentHeatingCoolingStateCharacteristics { get; set; }
    /// <summary>
    ///  Current Heating Cooling State | 当前制冷制热状态
    /// </summary>

    public HeatingCoolingState CurrentHeatingCoolingState { get; set; }
    /// <summary>
    /// Current Heating Cooling State change callback function | 当前制冷制热状态改变回调函数
    /// </summary>
    public event Action<HeatingCoolingState> CurrentHeatingCoolingStateSetValueCallBack;


    /// <summary>
    /// Target Heating Cooling State Characteristics |目标制冷制热状态特性
    /// </summary>
    public Characteristics TargetHeatingCoolingStateCharacteristics { get; set; }

    /// <summary>
    /// Target Heating Cooling State |目标制冷制热状态
    /// </summary>

    public HeatingCoolingState TargetHeatingCoolingState { get; set; }

    /// <summary>
    /// Target Heating Cooling State change callback function | 目标制冷制热状态改变回调函数
    /// </summary>
    public event Action<HeatingCoolingState> TargetHeatingCoolingStateSetValueCallBack;

    /// <summary>
    /// Temperature Display Units Characteristics|温度单位特性
    /// </summary>
    public Characteristics TemperatureDisplayUnitsCharacteristics { get; set; }
    /// <summary>
    /// Temperature Display Units|温度单位
    /// </summary>
    public TemperatureDisplayUnits TemperatureDisplayUnits { get; set; }
    /// <summary>
    /// Temperature Display Units change callback function|温度单位改变回调函数
    /// </summary>
    public event Action<TemperatureDisplayUnits> TemperatureDisplayUnitsSetValueCallBack;

    public AirConditioner(AccessoryDriver accessoryDriver, string name, int? aid = null) : base(accessoryDriver, name, aid)
    {
        Category = Category.CATEGORY_THERMOSTAT;
        this.CurrentTemperature = 25;
        this.TargetTemperature = 27;
        this.CurrentHeatingCoolingState = HeatingCoolingState.Cool;
        this.TargetHeatingCoolingState = HeatingCoolingState.Cool;
        this.TemperatureDisplayUnits = TemperatureDisplayUnits.Celsius;

        var service = AddPreloadService("Thermostat");
        //SetPrimaryService(service);
        //目标温度
        TargetTemperatureCharacteristics = service.GetCharacteristics("TargetTemperature");
        TargetTemperatureCharacteristics.SetValue(CurrentTemperature);
        TargetTemperatureCharacteristics.SetValueCallback = (o =>
        {
            this.TargetTemperature = Convert.ToSingle(o);
            Console.WriteLine("目标温度改变为:" + TargetTemperature+ ";The target temperature changes to:"+TargetTemperature);
           this.CurrentTemperatureCharacteristics.SetValue(TargetTemperature);
            TargetTemperatureSetValueCallBack?.Invoke(TargetTemperature);
         
        });

        TargetTemperatureCharacteristics.GetValueCallback = (() =>
        {
            Console.WriteLine("获取目标温度" + this.TargetTemperature+ ";get target temperature:"+this.TargetTemperature);
            return this.TargetTemperature;
        });
        //当前温度
        CurrentTemperatureCharacteristics = service.GetCharacteristics("CurrentTemperature");
        CurrentTemperatureCharacteristics.SetValue(TargetTemperature);
        CurrentTemperatureCharacteristics.SetValueCallback = (o =>
        {
            this.TargetTemperature = Convert.ToSingle(o);
            Console.WriteLine("目标温度改变为:" + TargetTemperature + ";The target temperature changes to:" + TargetTemperature);

            TargetTemperatureSetValueCallBack?.Invoke(TargetTemperature);
            //this.CurrentTemperature = Convert.ToSingle(o);
            //Console.WriteLine("当前温度改变为:" + TargetTemperature + ";The current temperature changes to:" + TargetTemperature);
            //CurrentTemperatureSetValueCallBack?.Invoke(this.CurrentTemperature);
        });

        CurrentTemperatureCharacteristics.GetValueCallback = (() =>
        {
            Console.WriteLine("获取目标温度" + this.TargetTemperature + ";get target temperature:" + this.TargetTemperature);
            return this.TargetTemperature;
            //Console.WriteLine("获取当前温度" + this.CurrentTemperature+";get current temperature:"+this.CurrentTemperature);
            //return this.CurrentTemperature;
        });
        //当前制冷制热状态
        CurrentHeatingCoolingStateCharacteristics = service.GetCharacteristics("CurrentHeatingCoolingState");
        CurrentHeatingCoolingStateCharacteristics.SetValueCallback = (o =>
        {
            this.TargetHeatingCoolingState = Enum.Parse<HeatingCoolingState>(o.ToString());
            Console.WriteLine("目标制冷制热状态改变为:" + TargetHeatingCoolingState.ToString() + ";target Heating Cooling State changes to:" + TargetHeatingCoolingState.ToString());
            TargetHeatingCoolingStateSetValueCallBack?.Invoke(this.TargetHeatingCoolingState);
            //this.CurrentHeatingCoolingState = Enum.Parse<HeatingCoolingState>(o.ToString());
            //Console.WriteLine("当前制冷制热状态改变为:" + CurrentHeatingCoolingState.ToString()+ ";Current Heating Cooling State changes to"+CurrentHeatingCoolingState.ToString());
            //CurrentHeatingCoolingStateSetValueCallBack?.Invoke(this.CurrentHeatingCoolingState);
        });

        CurrentHeatingCoolingStateCharacteristics.GetValueCallback = (() =>
        {
            Console.WriteLine("获取目标制冷制热状态:" + this.TargetHeatingCoolingState.ToString() + "get Target Heating Cooling State:" + this.TargetHeatingCoolingState);
            return (int)this.TargetHeatingCoolingState;
            //Console.WriteLine("获取当前制冷制热状态"+this.CurrentHeatingCoolingState.ToString() + "get Current Heating Cooling State" + this.CurrentHeatingCoolingState);
            //return (int)this.CurrentHeatingCoolingState;
        });

        //目标制冷制热状态特性
        TargetHeatingCoolingStateCharacteristics = service.GetCharacteristics("TargetHeatingCoolingState");
        TargetHeatingCoolingStateCharacteristics.SetValueCallback = (o =>
        {
            this.TargetHeatingCoolingState = Enum.Parse<HeatingCoolingState>(o.ToString());
            Console.WriteLine("目标制冷制热状态改变为:" + TargetHeatingCoolingState.ToString() + ";target Heating Cooling State changes to:" + TargetHeatingCoolingState.ToString());
            this.CurrentTemperatureCharacteristics.SetValue((int)TargetHeatingCoolingState);
            TargetHeatingCoolingStateSetValueCallBack?.Invoke(this.TargetHeatingCoolingState);
        });

        TargetHeatingCoolingStateCharacteristics.GetValueCallback = (() =>
        {
            Console.WriteLine("获取目标制冷制热状态:" + this.TargetHeatingCoolingState.ToString()+ "get Target Heating Cooling State:"+this.TargetHeatingCoolingState);
            return (int)this.TargetHeatingCoolingState;
        });

        TemperatureDisplayUnitsCharacteristics = service.GetCharacteristics("TemperatureDisplayUnits");
        TemperatureDisplayUnitsCharacteristics.SetValueCallback = (o =>
        {
            this.TemperatureDisplayUnits = Enum.Parse<TemperatureDisplayUnits>(o.ToString());
            Console.WriteLine("设置当前温度单位为" + TemperatureDisplayUnits.ToString()+ ";Temperature Display Units changes to:" + TemperatureDisplayUnits.ToString());
            TemperatureDisplayUnitsSetValueCallBack?.Invoke(this.TemperatureDisplayUnits);
        });

        TemperatureDisplayUnitsCharacteristics.GetValueCallback = (() =>
        {
            Console.WriteLine("获取当前温度单位"+this.TemperatureDisplayUnits+ ";get Temperature Display Units:"+ this.TemperatureDisplayUnits);
            return (int)this.TemperatureDisplayUnits;
        });
        //timer = new Timer(Test, default, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        //c.SetValue(1);
    }
}