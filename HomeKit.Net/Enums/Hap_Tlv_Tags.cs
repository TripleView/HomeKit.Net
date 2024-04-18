namespace HomeKit.Net.Enums;

/// <summary>
/// tlv tag enum;tlv tag enum;
/// </summary>
public enum Hap_Tlv_Tags : byte
{
    REQUEST_TYPE = 0,
    USERNAME = 1,
    SALT = 2,
    PUBLIC_KEY = 3,
    PASSWORD_PROOF = 4,
    ENCRYPTED_DATA = 5,
    SEQUENCE_NUM = 6,
    ERROR_CODE = 7,
    PROOF = 10,
    PERMISSIONS = 11
}