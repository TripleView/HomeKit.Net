using System.Net;

namespace HomeKit.Net;

/// <summary>
/// Class to store all (semi-)static information.That includes all needed for setup of driver and pairing;存储所有（半）静态信息的类。这包括设置驱动程序和配对所需的一切。
/// </summary>
public class State
{
    public int ConfigVersion { get; set; }

    public byte[] PrivateKey { get; set; }
    public byte[] PublicKey { get; set; }

    public string Mac { get; set; }
    public IPAddress Address { get; set; }
    public byte[] PinCode { get; set; }
    public int Port { get; set; }
    public Dictionary<Guid, byte[]> PairedClients { get; set; }

    public bool IsPaired => PairedClients.Count > 0;

    public Dictionary<Guid, Dictionary<string, byte>> ClientProperties { get; set; }

    private int ADMIN_BIT = 0x01;

    public string SetupId { set; get; }

    public string AccessoriesHash { get; set; }

    public State(IPAddress address, byte[] pinCode, int? port, string mac = null)
    {
        Address = address ?? Utils.GetIpAddress();
        Mac = string.IsNullOrWhiteSpace(mac) ? Utils.GenerateMac() : mac;
        PinCode = pinCode ?? Utils.GeneratePinCode();
        Port = port ?? Const.DEFAULT_PORT;
        SetupId = Utils.GenerateSetupId();
        ConfigVersion = 2;
        // Const.DEFAULT_CONFIG_VERSION;
        PairedClients = new Dictionary<Guid, byte[]>();
        ClientProperties = new Dictionary<Guid, Dictionary<string, byte>>();
        var keyPair = Utils.GenerateEd25519KeyPair();

        PrivateKey = keyPair.PrivateKey;
        PublicKey = keyPair.PublicKey;
    }

    public bool IsAdmin(Guid clientUuid)
    {
        if (!ClientProperties.ContainsKey(clientUuid))
        {
            return false;
        }

        var result = ClientProperties[clientUuid][Const.CLIENT_PROP_PERMS] & ADMIN_BIT;
        return result > 0;
    }

    public void AddPairedClient(Guid clientUuid, byte[] clientPublicKey, byte[] perms)
    {
        PairedClients[clientUuid] = clientPublicKey;
        // var str = Encoding.UTF8.GetString(perms);
        // Console.WriteLine($"Add Paired Client:{clientUuid.ToString()}");
        ClientProperties[clientUuid] = new Dictionary<string, byte>()
            {{Const.CLIENT_PROP_PERMS, perms.Length > 0 ? perms[0] : (byte) 0}};
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="clientUuid"></param>
    public void RemovePairedClient(Guid clientUuid)
    {
        PairedClients.Remove(clientUuid);
        ClientProperties.Remove(clientUuid);
        //all pairings must be removed when the last admin is removed
        if (!PairedClients.Any(it => IsAdmin(it.Key)))
        {
            PairedClients.Clear();
            ClientProperties.Clear();
        }
    }

    // def set_accessories_hash(self, accessories_hash):
    //     """Set the accessories hash and increment the config version if needed."""
    //     if self.accessories_hash == accessories_hash:
    // return False
    //     self.accessories_hash = accessories_hash
    //     self.increment_config_version()
    //     return True

    public void SetAccessoriesHash(string accessories_hash)
    {
    }

    public void IncrementConfigVersion()
    {
        ConfigVersion++;
        if (ConfigVersion > Const.MAX_CONFIG_VERSION)
        {
            ConfigVersion = 1;
        }
    }
}