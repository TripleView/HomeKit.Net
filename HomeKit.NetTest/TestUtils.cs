namespace HomeKit.NetTest;

public static class TestUtils
{
    public static byte[] GetSpecialStrToBytes(this string str)
    {
        return str.Replace("'", "").Split(",").Select(it => byte.Parse(it)).ToArray();
    }

}