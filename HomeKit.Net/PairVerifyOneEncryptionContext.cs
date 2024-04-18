namespace HomeKit.Net;

/// <summary>
/// Pair Verify One Encryption Context;配对验证阶段1的密钥上下文
/// </summary>
public class PairVerifyOneEncryptionContext
{
    public byte[] ClientPublic { get; set; }
    public byte[] PrivateKey { get; set; }
    public byte[] PublicKey { get; set; }
    public byte[] SharedKey { get; set; }
    public byte[] PreSessionKey { get; set; }
}