using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HomeKit.Net;

public static class Loader
{
    private static string serviceJsonPath;
    private static string characteristicsJsonPath;
    private static JObject serviceJObject;
    private static JObject characteristicsJObject;

    static Loader()
    {
        serviceJsonPath = Path.Combine(AppContext.BaseDirectory, "Resources", "services.json");
        characteristicsJsonPath = Path.Combine(AppContext.BaseDirectory, "Resources", "characteristics.json");
        var serviceJsonTextReader = new JsonTextReader(new StreamReader(serviceJsonPath));
        serviceJObject = (JObject)JObject.ReadFrom(serviceJsonTextReader);

        var characteristicsJsonTextReader = new JsonTextReader(new StreamReader(characteristicsJsonPath));
        characteristicsJObject = (JObject)JObject.ReadFrom(characteristicsJsonTextReader);
    }

    public static Service LoadService(string serviceName)
    {
        var serviceToken = serviceJObject[serviceName];
        if (serviceToken == null)
        {
            throw new Exception($"service:{serviceName} not exist");
        }

        var id = new Guid(serviceToken["UUID"].ToString());
        var characteristicsList = new List<Characteristics>();
        if (serviceToken["RequiredCharacteristics"] != null)
        {
            var requiredCharacteristicsList = (JArray)serviceToken["RequiredCharacteristics"];
            foreach (var jProperty in requiredCharacteristicsList)
            {
                var characteristicsName = jProperty.ToString();
                var characteristics = LoadCharacteristics(characteristicsName);
                if (characteristics != null)
                {
                    characteristics.Name = characteristicsName;
                    characteristicsList.Add(characteristics);
                }
            }
        }

        var service = new Service(id, serviceName, characteristicsList);
        foreach (var characteristics in service.CharacteristicsList)
        {
            characteristics.Service = service;
        }
        return service;
    }

    /// <summary>
    /// Load Characteristics
    /// 加载特征
    /// </summary>
    /// <param name="characteristicsName"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static Characteristics LoadCharacteristics(string characteristicsName)
    {
        var characteristicsToken = characteristicsJObject[characteristicsName];
        if (characteristicsToken == null)
        {
            throw new Exception($"characteristics:{characteristicsName} not exist");
        }
        var iid = new Guid(characteristicsToken["UUID"].ToString());
        var characteristics = characteristicsToken.ToObject<Characteristics>();
        if (characteristics != null)
        {
            characteristics.Iid = iid;
            characteristics.InitValue();
            characteristics.SetHapType();
        }

        var validValuesToken = characteristicsToken["ValidValues"] as JObject;
        if (validValuesToken != null)
        {
            var keyValuePairs = new List<KeyValuePair<string, string>>();

            foreach (var jProperty in validValuesToken.Properties())
            {
                var key = jProperty.Name;
                var value = jProperty.Value.ToString();

                keyValuePairs.Add(new KeyValuePair<string, string>(key, value));
            }

            characteristics.ValidValues = keyValuePairs;
        }
        // var characteristics=JsonConvert.DeserializeObject<Characteristics>(characteristicsToken)
        return characteristics;
    }
}