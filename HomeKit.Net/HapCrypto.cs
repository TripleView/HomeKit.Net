using System.Security.Cryptography;
using Newtonsoft.Json;
using NSec.Cryptography;

namespace HomeKit.Net;

public class HapCrypto
{
    private int LENGTH_LENGTH = 2;
    private int MIN_PAYLOAD_LENGTH = 1; //This is probably larger, but its only an optimization
    private int MIN_BLOCK_LENGTH => LENGTH_LENGTH + TAG_LENGTH + MIN_PAYLOAD_LENGTH;
    private byte[] CIPHER_SALT = "Control-Salt".ToBytes();
    private byte[] OUT_CIPHER_INFO = "Control-Read-Encryption-Key".ToBytes();
    private byte[] IN_CIPHER_INFO = "Control-Write-Encryption-Key".ToBytes();
    private int OutCount = 0;
    private int InCount = 0;
    private int TAG_LENGTH = 16;
    private int MAX_BLOCK_LENGTH = 1024;

    /// <summary>
    /// 缓冲区里的字节数组
    /// </summary>
    private List<byte> BytesInBuffer;

    private Key OutKey;
    private Key InKey;

    public HapCrypto(byte[] sharedKey)
    {
        BytesInBuffer = new List<byte>();
        SetKey(sharedKey);
    }

    public void SetKey(byte[] sharedKey)
    {
        var outHkdf = HKDF.DeriveKey(new HashAlgorithmName(nameof(SHA512)), sharedKey, 32,
            CIPHER_SALT,
            OUT_CIPHER_INFO);

        OutKey = Key.Import(AeadAlgorithm.ChaCha20Poly1305, outHkdf, KeyBlobFormat.RawSymmetricKey);

        var inHkdf = HKDF.DeriveKey(new HashAlgorithmName(nameof(SHA512)), sharedKey, 32,
            CIPHER_SALT,
            IN_CIPHER_INFO);

        InKey = Key.Import(AeadAlgorithm.ChaCha20Poly1305, inHkdf, KeyBlobFormat.RawSymmetricKey);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="bytes"></param>
    public void ReceiveData(byte[] bytes)
    {
        BytesInBuffer = bytes.ToList();
    }


    /// <summary>
    /// Decrypt and return any complete blocks in the buffer as plaintext,The received full cipher blocks are decrypted and returned and partial cipher blocks are buffered locally.
    /// 解密并以明文形式返回缓冲区中的任何完整块,接收到的完整密码块被解密并返回，部分密码块在本地缓冲。
    /// </summary>
    /// <returns></returns>
    public byte[] Decrypt()
    {
        var result = new List<byte>();
        // Console.WriteLine($"this.BytesInBuffer长度为:{this.BytesInBuffer.Count}");
        var origin = JsonConvert.DeserializeObject<List<byte>>(JsonConvert.SerializeObject(BytesInBuffer));
        while (BytesInBuffer.Count > MIN_BLOCK_LENGTH)
        {
            var blockLengthBytes = BytesInBuffer.Take(2).ToList();
            var blockSize = BitConverter.ToUInt16(blockLengthBytes.ToArray());

            var blockSizeWithLength = LENGTH_LENGTH + blockSize + TAG_LENGTH;
            if (BytesInBuffer.Count < blockSizeWithLength)
            {
                //Incoming buffer does not have the full block
                return result.ToArray();
            }

            BytesInBuffer = BytesInBuffer.Skip(LENGTH_LENGTH).ToList();
            var dataSize = blockSize + TAG_LENGTH;
            var nonce = BitConverter.GetBytes((ulong)InCount).PadTlsNonce();
            var decryptedData =
                AeadAlgorithm.ChaCha20Poly1305.Decrypt(InKey, nonce, blockLengthBytes.ToArray(),
                    BytesInBuffer.Take(dataSize).ToArray());

            if (decryptedData == null || decryptedData.Length == 0)
            {
                var d = 123;
            }
            result.AddRange(decryptedData);
            InCount += 1;
            BytesInBuffer = BytesInBuffer.Skip(dataSize).ToList();
            // var tt = decryptedData.GetString();
            // var d= string.Join(',', decryptedData);
        }

        return result.ToArray();
    }

    public byte[] Encrypt(byte[] bytes)
    {
        var result = new List<byte>();
        var offset = 0;
        var total = bytes.Length;
        while (offset < total)
        {
            var length = Math.Min(total - offset, MAX_BLOCK_LENGTH);
            var lengthBytes = BitConverter.GetBytes((ushort)length);
            var block = bytes[new Range(offset, offset + length)];
            var nonce = BitConverter.GetBytes((ulong)OutCount).PadTlsNonce();
            var encryptData = AeadAlgorithm.ChaCha20Poly1305.Encrypt(OutKey, nonce, lengthBytes,
                block);
            offset += length;
            OutCount += 1;
            result.AddRange(lengthBytes);
            result.AddRange(encryptData);
        }


        return result.ToArray();
    }
}