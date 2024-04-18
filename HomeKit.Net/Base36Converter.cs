namespace HomeKit.Net;

public static class Base36Converter
{
    private const int Base = 36;
    private const string Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public static string ConvertTo(long value)
    {
        string result = "";

        while (value > 0)
        {
            result = Chars[(int)(value % Base)] + result; // use StringBuilder for better performance
            value /= Base;
        }

        return result;
    }
}