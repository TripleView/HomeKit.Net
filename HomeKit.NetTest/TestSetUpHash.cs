using HomeKit.Net;


namespace HomeKit.NetTest;

public class TestSetUpHash
{
    [Fact]
    public void TestSetUpHashResult()
    {
        var mac = "13:82:6D:20:A5:11";
        var setupId = "W331";
        var str = setupId + mac;
        // str = "1JAB30:55:0B:13:FE:6F";
        var result = Utils.ToSha512ThenBase64(str);
        Assert.Equal("7g2hrA==",result);
    }
  
}