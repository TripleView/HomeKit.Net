

using HomeKit.Net;

namespace HomeKit.NetTest;

public class HapCryptoTest
{
    [Fact]
    public void TestDecrypt()
    {
        var bytes =
            "'75,0,124,105,147,139,139,219,65,173,192,253,158,193,123,19,202,141,45,32,227,168,43,88,252,235,214,18,214,47,214,65,26,14,158,110,49,230,53,158,81,171,189,103,178,194,102,34,129,116,17,223,154,132,249,85,74,154,22,219,184,46,74,158,117,225,64,197,27,5,25,30,75,156,158,143,156,89,235,151,168,83,203,253,247,86,26,164,249,98,31,57,113'"
                .GetSpecialStrToBytes();

        var shareKey =
            "'44,41,189,76,56,121,37,205,71,233,39,212,191,219,44,251,115,101,4,141,92,177,37,89,226,82,79,54,209,172,96,119'"
                .GetSpecialStrToBytes();
        var hapCrypto = new HapCrypto(shareKey);
        hapCrypto.ReceiveData(bytes);
        var result = hapCrypto.Decrypt();
        var resultBytes =
            "'71,69,84,32,47,97,99,99,101,115,115,111,114,105,101,115,32,72,84,84,80,47,49,46,49,13,10,72,111,115,116,58,32,77,121,84,101,109,112,83,101,110,115,111,114,92,48,51,50,65,52,51,48,56,65,46,95,104,97,112,46,95,116,99,112,46,108,111,99,97,108,13,10,13,10'"
                .GetSpecialStrToBytes();
        Assert.True(resultBytes.CompareTwoBytes(result));
    }

    [Fact]
    public void TestEncryptAndDecrypt()
    {
        var shareKey =
            "'99,67,232,71,208,27,133,177,10,9,92,204,88,237,104,205,155,27,54,214,124,115,231,37,167,13,4,32,142,237,98,106'"
                .GetSpecialStrToBytes();
        var hapCrypto = new HapCrypto(shareKey);
        var data = "'75,0,10,73,132,178,38,32,228,18,118,36,240,120,26,46,121,4,29,218,191,233,179,78,166,95,51,97,204,212,161,247,218,210,14,242,96,192,100,203,195,205,81,89,106,122,22,188,188,254,25,142,246,55,121,24,215,231,200,177,224,117,113,159,22,215,68,127,36,10,141,25,74,201,139,131,215,188,57,99,53,65,42,154,14,203,119,149,86,158,16,153,92'".GetSpecialStrToBytes();
        hapCrypto.ReceiveData(data);
        var decryptResult = hapCrypto.Decrypt();
        var decryptPythonResult = "'71,69,84,32,47,97,99,99,101,115,115,111,114,105,101,115,32,72,84,84,80,47,49,46,49,13,10,72,111,115,116,58,32,77,121,84,101,109,112,83,101,110,115,111,114,92,48,51,50,67,69,70,52,56,51,46,95,104,97,112,46,95,116,99,112,46,108,111,99,97,108,13,10,13,10'".GetSpecialStrToBytes();
        Assert.True(decryptResult.CompareTwoBytes(decryptPythonResult));
        
        var  enData="'72,84,84,80,47,49,46,49,32,50,48,48,32,79,75,13,10,67,111,110,116,101,110,116,45,84,121,112,101,58,32,97,112,112,108,105,99,97,116,105,111,110,47,104,97,112,43,106,115,111,110,13,10,67,111,110,116,101,110,116,45,76,101,110,103,116,104,58,32,54,54,51,13,10,13,10,123,34,97,99,99,101,115,115,111,114,105,101,115,34,58,91,123,34,97,105,100,34,58,49,44,34,115,101,114,118,105,99,101,115,34,58,91,123,34,105,105,100,34,58,49,44,34,116,121,112,101,34,58,34,51,69,34,44,34,99,104,97,114,97,99,116,101,114,105,115,116,105,99,115,34,58,91,123,34,105,105,100,34,58,50,44,34,116,121,112,101,34,58,34,49,52,34,44,34,112,101,114,109,115,34,58,91,34,112,119,34,93,44,34,102,111,114,109,97,116,34,58,34,98,111,111,108,34,125,44,123,34,105,105,100,34,58,51,44,34,116,121,112,101,34,58,34,50,48,34,44,34,112,101,114,109,115,34,58,91,34,112,114,34,93,44,34,102,111,114,109,97,116,34,58,34,115,116,114,105,110,103,34,44,34,118,97,108,117,101,34,58,34,34,125,44,123,34,105,105,100,34,58,52,44,34,116,121,112,101,34,58,34,50,49,34,44,34,112,101,114,109,115,34,58,91,34,112,114,34,93,44,34,102,111,114,109,97,116,34,58,34,115,116,114,105,110,103,34,44,34,118,97,108,117,101,34,58,34,34,125,44,123,34,105,105,100,34,58,53,44,34,116,121,112,101,34,58,34,50,51,34,44,34,112,101,114,109,115,34,58,91,34,112,114,34,93,44,34,102,111,114,109,97,116,34,58,34,115,116,114,105,110,103,34,44,34,118,97,108,117,101,34,58,34,77,121,84,101,109,112,83,101,110,115,111,114,34,125,44,123,34,105,105,100,34,58,54,44,34,116,121,112,101,34,58,34,51,48,34,44,34,112,101,114,109,115,34,58,91,34,112,114,34,93,44,34,102,111,114,109,97,116,34,58,34,115,116,114,105,110,103,34,44,34,118,97,108,117,101,34,58,34,100,101,102,97,117,108,116,34,125,44,123,34,105,105,100,34,58,55,44,34,116,121,112,101,34,58,34,53,50,34,44,34,112,101,114,109,115,34,58,91,34,112,114,34,93,44,34,102,111,114,109,97,116,34,58,34,115,116,114,105,110,103,34,44,34,118,97,108,117,101,34,58,34,34,125,93,125,44,123,34,105,105,100,34,58,56,44,34,116,121,112,101,34,58,34,56,65,34,44,34,99,104,97,114,97,99,116,101,114,105,115,116,105,99,115,34,58,91,123,34,105,105,100,34,58,57,44,34,116,121,112,101,34,58,34,49,49,34,44,34,112,101,114,109,115,34,58,91,34,112,114,34,44,34,101,118,34,93,44,34,102,111,114,109,97,116,34,58,34,102,108,111,97,116,34,44,34,109,105,110,86,97,108,117,101,34,58,45,50,55,51,46,49,44,34,109,105,110,83,116,101,112,34,58,48,46,49,44,34,109,97,120,86,97,108,117,101,34,58,49,48,48,48,44,34,117,110,105,116,34,58,34,99,101,108,115,105,117,115,34,44,34,118,97,108,117,101,34,58,50,53,46,48,125,93,125,93,125,93,125'"
            .GetSpecialStrToBytes();
        var encryptData = hapCrypto.Encrypt(enData);
        var encryptPythonData = "'227,2,138,101,160,44,214,30,241,161,95,49,176,184,103,14,96,169,210,113,72,28,211,4,76,144,195,236,170,214,72,173,50,114,131,27,230,11,223,95,251,141,116,146,122,72,158,65,218,188,205,245,232,156,12,243,238,103,199,243,140,93,239,13,108,126,89,111,127,147,126,166,75,65,182,119,65,67,66,58,183,220,61,97,100,230,132,113,152,169,93,250,43,245,251,111,73,163,217,110,199,117,42,25,253,85,158,81,92,253,152,67,79,233,5,194,162,17,121,161,133,188,139,205,212,118,180,18,229,39,30,64,229,2,89,154,100,137,244,15,133,115,151,96,166,182,251,169,88,227,209,72,28,159,62,239,223,216,199,107,188,61,6,99,128,78,191,125,13,244,32,12,186,64,255,26,140,221,86,120,81,167,169,95,161,176,52,118,192,183,135,110,92,98,184,76,113,192,211,233,139,255,191,214,169,80,24,217,210,203,149,201,137,56,169,126,239,123,96,168,113,225,15,117,228,203,137,218,155,155,227,4,198,145,86,83,68,180,97,53,114,204,193,100,64,76,154,206,103,12,42,21,93,41,12,186,95,16,96,235,8,107,7,140,160,92,46,94,212,69,160,30,203,200,174,159,126,24,217,248,31,172,131,133,177,176,115,65,88,238,254,153,227,163,49,15,126,178,35,42,226,195,168,22,113,248,17,183,134,65,30,204,72,35,108,15,104,151,246,232,49,249,101,133,229,225,230,115,78,134,78,163,136,239,188,158,105,230,64,233,224,6,79,30,42,83,145,179,32,125,221,238,119,247,217,4,5,186,66,46,55,137,35,171,97,28,171,188,172,161,78,234,240,32,139,51,142,210,231,11,172,117,145,97,83,124,137,16,217,224,95,0,165,168,28,21,247,97,214,91,64,98,176,156,102,245,190,227,219,57,90,190,70,182,249,143,136,112,24,99,61,242,223,17,241,198,72,59,171,153,129,203,65,116,105,80,253,208,103,124,249,138,168,23,2,98,204,230,78,43,155,63,147,133,15,68,37,135,224,215,128,189,162,146,52,191,41,56,249,242,244,58,217,159,170,250,196,229,163,23,84,220,205,119,152,87,159,232,148,140,205,5,87,172,133,233,16,118,208,252,225,46,91,122,7,44,202,216,25,17,143,34,158,173,55,137,143,122,141,192,97,64,160,120,244,197,82,8,152,98,59,219,124,179,36,144,95,178,229,42,142,89,76,221,60,83,182,105,177,69,249,50,205,214,244,64,222,44,161,95,70,88,176,166,96,26,99,215,248,26,243,179,186,95,75,252,209,39,13,157,22,68,203,116,46,116,124,197,236,185,138,219,25,197,246,53,109,16,163,138,27,208,142,170,76,112,157,158,49,131,183,159,228,20,99,218,75,168,140,90,125,197,166,174,146,78,70,106,21,151,79,159,215,72,241,114,7,218,108,73,164,231,3,224,166,2,231,155,134,8,75,166,192,144,246,29,184,100,18,233,83,0,117,0,123,142,93,91,133,154,193,254,93,195,78,14,76,75,130,98,18,130,59,245,215,74,159,42,41,223,172,206,121,4,100,47,27,77,163,68,219,69,252,188,95,171,54,26,154,236,172,178,108,245,13,101,91,57,66,30,13,110,94,57,251,177,103,9,35,52,217,156,50,201,32,180,115,171,35,153,62,178,59,110,45,215,139,22,199,187,199,216,65,98,114,8,35'".GetSpecialStrToBytes();
        Assert.True(encryptData.CompareTwoBytes(encryptPythonData));
    }

    [Fact]
    public void TestEncrypt()
    {
        var bytes =
            "'72,84,84,80,47,49,46,49,32,50,48,48,32,79,75,13,10,67,111,110,116,101,110,116,45,84,121,112,101,58,32,97,112,112,108,105,99,97,116,105,111,110,47,104,97,112,43,106,115,111,110,13,10,67,111,110,116,101,110,116,45,76,101,110,103,116,104,58,32,54,54,51,13,10,13,10,123,34,97,99,99,101,115,115,111,114,105,101,115,34,58,91,123,34,97,105,100,34,58,49,44,34,115,101,114,118,105,99,101,115,34,58,91,123,34,105,105,100,34,58,49,44,34,116,121,112,101,34,58,34,51,69,34,44,34,99,104,97,114,97,99,116,101,114,105,115,116,105,99,115,34,58,91,123,34,105,105,100,34,58,50,44,34,116,121,112,101,34,58,34,49,52,34,44,34,112,101,114,109,115,34,58,91,34,112,119,34,93,44,34,102,111,114,109,97,116,34,58,34,98,111,111,108,34,125,44,123,34,105,105,100,34,58,51,44,34,116,121,112,101,34,58,34,50,48,34,44,34,112,101,114,109,115,34,58,91,34,112,114,34,93,44,34,102,111,114,109,97,116,34,58,34,115,116,114,105,110,103,34,44,34,118,97,108,117,101,34,58,34,34,125,44,123,34,105,105,100,34,58,52,44,34,116,121,112,101,34,58,34,50,49,34,44,34,112,101,114,109,115,34,58,91,34,112,114,34,93,44,34,102,111,114,109,97,116,34,58,34,115,116,114,105,110,103,34,44,34,118,97,108,117,101,34,58,34,34,125,44,123,34,105,105,100,34,58,53,44,34,116,121,112,101,34,58,34,50,51,34,44,34,112,101,114,109,115,34,58,91,34,112,114,34,93,44,34,102,111,114,109,97,116,34,58,34,115,116,114,105,110,103,34,44,34,118,97,108,117,101,34,58,34,77,121,84,101,109,112,83,101,110,115,111,114,34,125,44,123,34,105,105,100,34,58,54,44,34,116,121,112,101,34,58,34,51,48,34,44,34,112,101,114,109,115,34,58,91,34,112,114,34,93,44,34,102,111,114,109,97,116,34,58,34,115,116,114,105,110,103,34,44,34,118,97,108,117,101,34,58,34,100,101,102,97,117,108,116,34,125,44,123,34,105,105,100,34,58,55,44,34,116,121,112,101,34,58,34,53,50,34,44,34,112,101,114,109,115,34,58,91,34,112,114,34,93,44,34,102,111,114,109,97,116,34,58,34,115,116,114,105,110,103,34,44,34,118,97,108,117,101,34,58,34,34,125,93,125,44,123,34,105,105,100,34,58,56,44,34,116,121,112,101,34,58,34,56,65,34,44,34,99,104,97,114,97,99,116,101,114,105,115,116,105,99,115,34,58,91,123,34,105,105,100,34,58,57,44,34,116,121,112,101,34,58,34,49,49,34,44,34,112,101,114,109,115,34,58,91,34,112,114,34,44,34,101,118,34,93,44,34,102,111,114,109,97,116,34,58,34,102,108,111,97,116,34,44,34,109,105,110,83,116,101,112,34,58,48,46,49,44,34,117,110,105,116,34,58,34,99,101,108,115,105,117,115,34,44,34,109,105,110,86,97,108,117,101,34,58,45,50,55,51,46,49,44,34,109,97,120,86,97,108,117,101,34,58,49,48,48,48,44,34,118,97,108,117,101,34,58,50,50,46,48,125,93,125,93,125,93,125'"
                .GetSpecialStrToBytes();
        
        var shareKey =
            "'44,41,189,76,56,121,37,205,71,233,39,212,191,219,44,251,115,101,4,141,92,177,37,89,226,82,79,54,209,172,96,119'"
                .GetSpecialStrToBytes();
        var hapCrypto = new HapCrypto(shareKey);
        hapCrypto.ReceiveData(bytes);
        var result = hapCrypto.Encrypt(bytes);
        var resultBytes =
            "'227,2,247,18,83,188,47,167,109,135,96,177,15,50,75,14,47,231,80,10,3,67,229,252,67,149,26,162,254,14,197,140,236,200,21,66,144,158,4,68,154,174,197,228,70,46,254,211,249,73,65,77,8,182,18,126,195,210,244,214,117,163,248,101,53,50,106,205,151,245,202,187,250,243,0,8,190,116,70,7,10,164,139,159,136,14,161,129,128,172,218,143,64,239,70,90,20,227,17,225,161,224,38,89,9,212,215,159,165,70,69,118,103,155,73,37,5,161,191,84,180,161,162,93,64,157,208,143,53,251,64,93,157,90,11,143,228,42,225,141,156,27,86,197,49,230,100,38,38,220,80,152,158,195,120,153,65,67,252,149,169,251,19,228,14,173,215,77,152,7,229,176,171,27,20,156,66,211,122,255,170,116,42,245,80,153,229,133,75,184,247,173,69,208,236,67,229,134,150,243,47,60,210,165,58,247,104,203,159,54,79,45,179,151,65,199,160,58,2,195,82,218,97,211,184,186,189,225,60,162,156,182,1,22,82,169,123,7,112,9,145,204,23,158,170,132,194,104,199,202,234,186,182,112,70,227,168,77,223,12,120,219,165,205,199,135,128,141,109,113,148,54,201,4,24,33,195,223,36,239,2,40,230,175,110,76,164,127,45,96,208,154,129,22,80,175,41,138,175,139,210,204,77,114,165,209,223,241,127,239,140,212,52,68,9,160,141,57,71,7,201,210,44,98,1,11,144,131,225,35,9,174,198,169,211,97,95,157,97,108,174,40,171,173,25,249,46,114,176,64,80,80,65,170,78,0,183,57,121,167,250,180,128,198,185,41,31,129,219,81,129,89,180,173,12,202,17,116,13,216,229,14,187,169,159,250,88,64,102,133,43,253,117,227,45,223,227,5,52,64,109,107,147,83,147,117,110,254,42,204,192,165,80,83,35,241,6,172,63,186,38,132,47,93,7,89,116,202,134,226,15,216,119,221,212,117,253,53,178,150,6,214,11,223,164,196,60,73,212,225,53,95,2,175,61,89,33,14,64,146,103,172,86,143,52,237,208,8,78,185,126,207,17,9,85,117,110,171,221,70,226,218,165,236,194,12,112,43,113,161,102,119,56,124,129,187,84,20,43,161,75,62,0,217,162,123,91,140,198,21,139,12,54,65,83,4,16,69,9,222,181,94,239,77,212,163,33,36,184,43,151,29,135,82,120,108,164,20,134,227,165,77,14,2,35,115,110,59,233,12,0,34,108,202,178,239,93,172,193,121,179,178,234,67,183,236,213,78,199,128,97,21,218,116,10,37,249,143,55,70,236,233,102,198,192,47,142,224,123,92,151,40,31,79,215,122,131,7,34,110,164,92,2,228,246,102,211,222,31,56,225,246,35,140,200,228,136,236,131,240,112,104,241,251,122,21,161,66,17,139,18,87,136,212,185,73,117,112,104,169,157,237,28,44,37,252,125,88,84,173,22,149,100,195,142,92,226,103,161,51,179,234,130,217,29,76,165,43,182,231,168,8,165,195,69,37,44,25,111,73,221,205,35,242,109,157,226,92,151,181,218,23,77,233,110,224,181,5,141,65,84,10,218,244,48,165,187,150,45,129,147,57,24,46,165,73,180,115,231,222,122,24,21,236,72,190,172,170,114,201,47,86,196,45,122,169,54,202,54,86,199,236,85,166,164,242,25,12,141,111,237,145,0,161,227,31,141'"
                .GetSpecialStrToBytes();
        Assert.True(resultBytes.CompareTwoBytes(result));
    }
}