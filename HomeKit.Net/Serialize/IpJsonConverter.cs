﻿using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HomeKit.Net.Serialize;

public class IPAddressConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType.IsAssignableTo(typeof(IPAddress));
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString());
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        return IPAddress.Parse((string)reader.Value);
    }
}

public class IPEndPointConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return (objectType == typeof(IPEndPoint));
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        IPEndPoint ep = (IPEndPoint)value;
        JObject jo = new JObject();
        jo.Add("Address", JToken.FromObject(ep.Address, serializer));
        jo.Add("Port", ep.Port);
        jo.WriteTo(writer);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);
        IPAddress address = jo["Address"].ToObject<IPAddress>(serializer);
        int port = (int)jo["Port"];
        return new IPEndPoint(address, port);
    }
}