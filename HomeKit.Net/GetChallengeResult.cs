using System.Numerics;

namespace HomeKit.Net;

public class GetChallengeResult
{
    public byte[] Salt { get; set; }
    public BigInteger B { get; set; }
}