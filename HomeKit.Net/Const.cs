namespace HomeKit.Net;

public static class Const
{
    public static string BASE_UUID = "-0000-1000-8000-0026BB765291";

    // Standalone accessory ID (i.e. not bridged)
    public static int STANDALONE_AID = 1;

    //Default values
    public static int DEFAULT_CONFIG_VERSION = 1;
    public static int DEFAULT_PORT = 51827;

    //Configuration version
    public static int MAX_CONFIG_VERSION = 65535;

    //HAP Permissions 
    public static string HAP_PERMISSION_HIDDEN = "hd";
    public static string HAP_PERMISSION_NOTIFY = "ev";
    public static string HAP_PERMISSION_READ = "pr";
    public static string HAP_PERMISSION_WRITE = "pw";
    public static string HAP_PERMISSION_WRITE_RESPONSE = "wr";

    // HAP representation 
    public static string HAP_REPR_ACCS = "accessories";
    public static string HAP_REPR_AID = "aid";
    public static string HAP_REPR_CHARS = "characteristics";
    public static string HAP_REPR_DESC = "description";
    public static string HAP_REPR_FORMAT = "format";
    public static string HAP_REPR_IID = "iid";
    public static string HAP_REPR_MAX_LEN = "maxLen";
    public static string HAP_REPR_PERM = "perms";
    public static string HAP_REPR_PID = "pid";
    public static string HAP_REPR_PRIMARY = "primary";
    public static string HAP_REPR_SERVICES = "services";
    public static string HAP_REPR_LINKED = "linked";
    public static string HAP_REPR_STATUS = "status";
    public static string HAP_REPR_TTL = "ttl";
    public static string HAP_REPR_TYPE = "type";
    public static string HAP_REPR_VALUE = "value";
    public static string HAP_REPR_VALID_VALUES = "valid-values";

    public static string HAP_PROTOCOL_VERSION = "01.01.00";
    public static string HAP_PROTOCOL_SHORT_VERSION = "1.1";

    //Client properties
    public static string CLIENT_PROP_PERMS = "permissions";

    //HAP Format
    public static string HAP_FORMAT_BOOL = "bool";
    public static string HAP_FORMAT_INT = "int";
    public static string HAP_FORMAT_FLOAT = "float";
    public static string HAP_FORMAT_STRING = "string";
    public static string HAP_FORMAT_ARRAY = "array";
    public static string HAP_FORMAT_DICTIONARY = "dictionary";
    public static string HAP_FORMAT_UINT8 = "uint8";
    public static string HAP_FORMAT_UINT16 = "uint16";
    public static string HAP_FORMAT_UINT32 = "uint32";
    public static string HAP_FORMAT_UINT64 = "uint64";
    public static string HAP_FORMAT_DATA = "data";
    public static string HAP_FORMAT_TLV8 = "tlv8";

    public static List<string> HAP_FORMAT_NUMERICS = new List<string>()
    {
        HAP_FORMAT_INT,
        HAP_FORMAT_FLOAT,
        HAP_FORMAT_UINT8,
        HAP_FORMAT_UINT16,
        HAP_FORMAT_UINT32,
        HAP_FORMAT_UINT64,
    };

    public static string HAP_SERVICE_TYPE = "_hap._tcp.";

    //srp init Data
    public static string SrpNStr =
        "FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD129024E088A67CC74020BBEA63B139B22514A08798E3404DDEF9519B3CD3A431B302B0A6DF25F14374FE1356D6D51C245E485B576625E7EC6F44C42E9A637ED6B0BFF5CB6F406B7EDEE386BFB5A899FA5AE9F24117C4B1FE649286651ECE45B3DC2007CB8A163BF0598DA48361C55D39A69163FA8FD24CF5F83655D23DCA3AD961C62F356208552BB9ED529077096966D670C354E4ABC9804F1746C08CA18217C32905E462E36CE3BE39E772C180E86039B2783A2EC07A28FB5C55DF06F4C52C9DE2BCBF6955817183995497CEA956AE515D2261898FA051015728E5A8AAAC42DAD33170D04507A33A85521ABDF1CBA64ECFB850458DBEF0A8AEA71575D060C7DB3970F85A6E1E4C7ABF5AE8CDB0933D71E8C94E04A25619DCEE3D2261AD2EE6BF12FFA06D98A0864D87602733EC86A64521F2B18177B200CBBE117577A615D6C770988C0BAD946E208E24FA074E5AB3143DB5BFCE0FD108E4B82D120A93AD2CAFFFFFFFFFFFFFFFF";

    public static string SrpGStr = "5";

    /// <summary>
    /// pairing response header type
    /// </summary>
    public static string PAIRING_RESPONSE_TYPE = "application/pairing+tlv8";

    public static byte[] PAIRING_3_SALT = "Pair-Setup-Encrypt-Salt".ToBytes();
    public static byte[] PAIRING_3_INFO = "Pair-Setup-Encrypt-Info".ToBytes();

    public static byte[] PAIRING_3_NONCE = "PS-Msg05".ToBytes().PadTlsNonce();

    public static byte[] PAIRING_4_SALT = "Pair-Setup-Controller-Sign-Salt".ToBytes();
    public static byte[] PAIRING_4_INFO = "Pair-Setup-Controller-Sign-Info".ToBytes();

    public static byte[] PAIRING_5_SALT = "Pair-Setup-Accessory-Sign-Salt".ToBytes();
    public static byte[] PAIRING_5_INFO = "Pair-Setup-Accessory-Sign-Info".ToBytes();
    public static byte[] PAIRING_5_NONCE = "PS-Msg06".ToBytes().PadTlsNonce();

    public static byte[] PVERIFY_1_SALT = "Pair-Verify-Encrypt-Salt".ToBytes();
    public static byte[] PVERIFY_1_INFO = "Pair-Verify-Encrypt-Info".ToBytes();
    public static byte[] PVERIFY_1_NONCE = "PV-Msg02".ToBytes().PadTlsNonce();
    public static byte[] PVERIFY_2_NONCE = "PV-Msg03".ToBytes().PadTlsNonce();

    public static int DEFAULT_MAX_LENGTH = 64;
    public static int ABSOLUTE_MAX_LENGTH = 256;

    public static string JSON_RESPONSE_TYPE = "application/hap+json";


    public static Dictionary<string, object> HAP_FORMAT_DEFAULTS = new Dictionary<string, object>()
    {
        {HAP_FORMAT_BOOL, false},
        {HAP_FORMAT_INT, 0},
        {HAP_FORMAT_FLOAT, 0},
        {HAP_FORMAT_STRING, ""},
        {HAP_FORMAT_ARRAY, ""},
        {HAP_FORMAT_DICTIONARY, ""},
        {HAP_FORMAT_UINT8, 0},
        {HAP_FORMAT_UINT16, 0},
        {HAP_FORMAT_UINT32, 0},
        {HAP_FORMAT_UINT64, 0},
        {HAP_FORMAT_DATA, ""},
        {HAP_FORMAT_TLV8, ""}
    };

    public static bool Flag = false;

    private static Guid CHAR_BUTTON_EVENT = new Guid("00000126-0000-1000-8000-0026BB765291");
    private static Guid CHAR_PROGRAMMABLE_SWITCH_EVENT = new Guid("00000073-0000-1000-8000-0026BB765291");

    public static List<Guid> IMMEDIATE_NOTIFY = new List<Guid>() { CHAR_BUTTON_EVENT, CHAR_PROGRAMMABLE_SWITCH_EVENT };

    public static string GetTopic(int aid, int iid)
    {
        return $"{aid}.{iid}";
    }
}