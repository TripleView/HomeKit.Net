namespace HomeKit.Net;

public class Tlv
{
    public byte[] Encode(List<TlvItem> items)
    {
        var result = new List<byte>();
        foreach (var tlvItem in items)
        {
            var dataLength = tlvItem.Value.Length;
            var bytes = new List<byte>();
            if (dataLength <= 255)
            {
                bytes.AddRange(tlvItem.Tag);
                bytes.AddRange(new byte[] { (byte)dataLength });
                bytes.AddRange(tlvItem.Value);
            }
            else
            {
                for (int i = 0; i < dataLength / 255; i++)
                {
                    bytes.AddRange(tlvItem.Tag);
                    bytes.Add(255);
                    var datas = tlvItem.Value[new Range(i * 255, (i + 1) * 255)];
                    bytes.AddRange(datas);
                }

                var remaining = dataLength % 255;
                bytes.AddRange(tlvItem.Tag);
                bytes.AddRange(new byte[] { (byte)remaining });
                bytes.AddRange(tlvItem.Value[new Range(tlvItem.Value.Length - remaining, tlvItem.Value.Length)]);
            }
            result.AddRange(bytes);
        }

        return result.ToArray();
    }

    public List<TlvItem> Decode(byte[] data)
    {
        // Console.WriteLine($"tlv 长度为{data.Length}");
        var current = 0;
        var result = new List<TlvItem>();
        while (current < data.Length)
        {
            var tag = data[new Range(current, current + 1)];
            var length = data[current + 1];
            var value = data[new Range(current + 2, current + 2 + length)];
            var target = result.FirstOrDefault(it => it.Tag.CompareTwoBytes(tag));
            if (target != null)
            {
                var temp = target.Value.ToList();
                temp.AddRange(value);
                target.Value = temp.ToArray();
            }
            else
            {
                result.Add(new TlvItem(tag, value));
            }

            current = current + 2 + length;
        }

        return result;
    }
}

public class TlvItem
{
    public TlvItem(byte[] tag, byte[] value)
    {
        Tag = tag;
        Value = value;
    }
    public byte[] Tag { get; set; }
    public byte[] Value { get; set; }
}