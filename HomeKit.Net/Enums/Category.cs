namespace HomeKit.Net.Enums;

/// <summary>
/// Category is a hint to iOS clients about what "type" of Accessory this represents, for UI only.This is for the icon in the iOS Home app.
/// Category 是对 iOS 客户端的提示，提示这是什么“类型”的配件，仅用于 UI。这是用于展示 iOS Home 应用程序中配件的图标。
/// </summary>
public enum Category
{
    CATEGORY_OTHER = 1,
    CATEGORY_BRIDGE = 2,
    CATEGORY_FAN = 3,
    CATEGORY_GARAGE_DOOR_OPENER = 4,
    CATEGORY_LIGHTBULB = 5,
    CATEGORY_DOOR_LOCK = 6,
    CATEGORY_OUTLET = 7,
    CATEGORY_SWITCH = 8,
    CATEGORY_THERMOSTAT = 9,
    CATEGORY_SENSOR = 10,
    CATEGORY_ALARM_SYSTEM = 11,
    CATEGORY_DOOR = 12,
    CATEGORY_WINDOW = 13,
    CATEGORY_WINDOW_COVERING = 14,
    CATEGORY_PROGRAMMABLE_SWITCH = 15,
    CATEGORY_RANGE_EXTENDER = 16,
    CATEGORY_CAMERA = 17,
    CATEGORY_VIDEO_DOOR_BELL = 18,
    CATEGORY_AIR_PURIFIER = 19,
    CATEGORY_HEATER = 20,
    CATEGORY_AIR_CONDITIONER = 21,
    CATEGORY_HUMIDIFIER = 22,
    CATEGORY_DEHUMIDIFIER = 23,
    CATEGORY_SPEAKER = 26,
    CATEGORY_SPRINKLER = 28,
    CATEGORY_FAUCET = 29,
    CATEGORY_SHOWER_HEAD = 30,
    CATEGORY_TELEVISION = 31,
    CATEGORY_TARGET_CONTROLLER = 32  // Remote Controller
}