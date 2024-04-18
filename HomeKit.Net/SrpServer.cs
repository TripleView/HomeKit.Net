using System.Numerics;
using System.Text;

namespace HomeKit.Net;

/// <summary>
/// b    Secret ephemeral values (long)
/// A    Public ephemeral values (long)
/// Ab   Public ephemeral values (bytes)
/// B    Public ephemeral values (long)
/// Bb   Public ephemeral values (bytes)
/// g    A generator modulo N (long)
/// gb   A generator modulo N (bytes)
/// I    Username (bytes)
/// k    Multiplier parameter (long)
/// N    Large safe prime (long)
/// Nb   Large safe prime (bytes)
/// p    Cleartext Password (bytes)
/// s    Salt (bytes)
/// u    Random scrambling parameter (bytes)
/// v    Password verifier (long)
/// srp server;srp服务器
/// </summary>
public class SrpServer
{
    public Func<byte[], byte[]> HashFunc;
    public BigInteger N;
    public byte[] Nb => N.ToByteArray(true, true);
    public BigInteger g;
    public byte[] gb => g.ToByteArray(true, true);

    public int Nlen;
    public byte[] s;
    public byte[] I;
    public byte[] p;
    public BigInteger v;
    public BigInteger k;
    public BigInteger b;
    public BigInteger B;
    public byte[] Bb => B.ToByteArray(true, true);
    public BigInteger A;
    public byte[] Ab => A.ToByteArray(true, true);
    public BigInteger S;
    public byte[] Sb => S.ToByteArray(true, true);
    public BigInteger K;
    public byte[] Kb => K.ToByteArray(true, true);
    public byte[] M;
    public BigInteger u;

    public byte[] HAMK;

    public SrpServer(Func<byte[], byte[]> hashFunc, byte[] i, byte[] p, byte[]? salt = null, BigInteger? v = null,
        BigInteger? b = null)
    {
        HashFunc = hashFunc;

        N = new BigInteger(Const.SrpNStr.ToHexByte(), true, true);
        g = new BigInteger(int.Parse(Const.SrpGStr));
        Nlen = 3072;
        s = salt ?? GenerateSalt();
        I = i;
        this.p = p;
        this.v = v ?? GetVerifier();
        k = Getk();
        this.b = b ?? Generateb();
        B = DeriveB();
    }

    public BigInteger Getk()
    {
        var bytes = MergeBytes(Nb, PadN(gb));
        bytes = HashFunc(bytes);
        var result = new BigInteger(bytes, true, true);
        return result;
    }

    public BigInteger GetBigK()
    {
        var SbBytes = HashFunc(Sb);
        var result = new BigInteger(SbBytes, true, true);
        return result;
    }

    public void SetA(byte[] aValue)
    {
        var A = new BigInteger(aValue, true, true);
        this.A = A;
        S = DerivePremasterSecret();
        K = GetBigK();
        M = GetM();
        HAMK = GetHAMK();
    }

    public byte[] Verify(byte[] MValue)
    {
        return CompareTwoBytes(M, MValue) ? HAMK : Array.Empty<byte>();
    }

    public BigInteger GetSessionKey()
    {
        return K;
    }

    public BigInteger DeriveB()
    {
        var result = (k * v + BigInteger.ModPow(g, b, N)) % N;
        return result;
    }

    public byte[] GetHAMK()
    {
        var mBytes = MergeBytes(Ab, M, Kb);
        return HashFunc(mBytes);
    }

    public byte[] GetM()
    {
        var gb = this.gb;
        var nb = Nb;
        var hN = HashFunc(nb);
        var hG = HashFunc(gb);
        var hGroup = new List<byte>();
        for (int i = 0; i < hN.Length; i++)
        {
            var b = (byte)(hN[i] ^ hG[i]);
            hGroup.Add(b);
        }

        var hU = HashFunc(I);
        hGroup.AddRange(hU);
        hGroup.AddRange(s);
        hGroup.AddRange(Ab);
        hGroup.AddRange(Bb);
        hGroup.AddRange(Kb);
        var result = HashFunc(hGroup.ToArray());
        return new BigInteger(result, true, true).ToByteArray(true, true);
    }

    public BigInteger DerivePremasterSecret()
    {
        var uBytes = MergeBytes(PadN(Ab), PadN(Bb));
        uBytes = HashFunc(uBytes);
        u = new BigInteger(uBytes, true, true);
        var Avu = A * BigInteger.ModPow(v, u, N);
        var result = BigInteger.ModPow(Avu, b, N);
        return result;
    }


    public bool TestCompareString(string b, byte[] source)
    {
        b = b.Replace("'", "");
        var bytes = b.Split(",").Select(it => byte.Parse(it)).ToArray();
        return CompareTwoBytes(bytes, source);
    }

    private byte[] PadN(byte[] bytes)
    {
        var len = Nlen / 8;
        if (bytes.Length > len)
        {
            return bytes;
        }
        else
        {
            var loss = len - bytes.Length;
            var resultList = new List<byte>();
            for (int i = 0; i < loss; i++)
            {
                resultList.Add(0);
            }

            var result = MergeBytes(resultList.ToArray(), bytes);
            return result;
        }
    }

    public BigInteger GetVerifier()
    {
        var privateKey = GetPrivateKey();
        var result = BigInteger.ModPow(g, privateKey, N);
        return result;
    }

    public BigInteger GetPrivateKey()
    {
        var baseInfo = MergeBytes(I, Encoding.UTF8.GetBytes(":"), p);
        var hashResult = HashFunc(baseInfo);
        var middleData = MergeBytes(s, hashResult);
        var hashResult2 = HashFunc(middleData);
        var result = new BigInteger(hashResult2, true, true);
        return result;
    }

    // def get_challenge(self):
    //     return (self.s, self.B)
    public GetChallengeResult GetChallenge()
    {
        return new GetChallengeResult()
        {
            Salt = s,
            B = B
        };
    }

    public bool CompareTwoBytes(byte[] b1, byte[] b2)
    {
        return b1.CompareTwoBytes(b2);
    }

    private byte[] MergeBytes(params byte[][] bytes)
    {
        return Utils.MergeBytes(bytes);
    }

    /// <summary>
    /// generate salt
    /// </summary>
    /// <returns></returns>
    private byte[] GenerateSalt()
    {
        var randon = new Random();
        var result = new byte[16];
        randon.NextBytes(result);
        return result;
    }

    private BigInteger Generateb()
    {
        var randon = new Random();
        var result = new byte[32];
        randon.NextBytes(result);
        var value = new BigInteger(result, true, true);
        return value;
    }


}