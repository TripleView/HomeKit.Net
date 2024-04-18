using System.Security.Cryptography;
using HomeKit.Net;
using HomeKit.Net.Enums;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NSec.Cryptography;

namespace HomeKit.Net.HttpServer;

public class HapHandler
{
    private ILogger<HapHandler> logger = LoggerFactory.Create(it => it.AddConsole()).CreateLogger<HapHandler>();
    public AccessoryDriver Driver { get; }

    public HapCrypto HapCrypto { get; set; }

    public PairVerifyOneEncryptionContext PairVerifyOneEncryptionContext { get; set; }

    public HapHandler(AccessoryDriver driver)
    {
        Driver = driver;
        PairVerifyOneEncryptionContext = new PairVerifyOneEncryptionContext();
    }

    public async Task SendAuthenticationErrorTlvResponse(HttpContext e, Hap_Tlv_States state)
    {
        Console.WriteLine("未认证");
        e.Response.StatusCode = StatusCode.Status200OK;
        var tlvParams = new List<TlvItem>();
        tlvParams.Add(new TlvItem(new byte[] { (byte)Hap_Tlv_Tags.SEQUENCE_NUM },
            new byte[] { (byte)state }));
        tlvParams.Add(new TlvItem(new byte[] { (byte)Hap_Tlv_Tags.ERROR_CODE },
            new byte[] { (byte)Hap_Tlv_Errors.AUTHENTICATION }));

        var c2 = new Tlv().Encode(tlvParams);
        var length = c2.Length;
        // Console.WriteLine($"length:{length}");
        e.Response.ContentLength = length;
        e.Response.ContentType = Const.PAIRING_RESPONSE_TYPE;
        e.Response.Write(c2);
        return;
    }

    public async Task PairSetup(HttpContext e)
    {
        try
        {
            var c = e.Request;
            using (var stream = new MemoryStream())
            {
                await e.Request.Body.CopyToAsync(stream);
                stream.Seek(0, SeekOrigin.Begin);
                var result = new Tlv().Decode(stream.ToArray());
                var sequenceItem = result.FirstOrDefault(it => it.Tag[0] == 6);
                var tlvParams = new List<TlvItem>();

                if (sequenceItem == null || sequenceItem.Value.Length == 0)
                {
                    e.Response.StatusCode = StatusCode.Status200OK;
                    tlvParams.Add(new TlvItem(new byte[] { (byte)Hap_Tlv_Tags.SEQUENCE_NUM },
                        new byte[] { (byte)Hap_Tlv_States.M2 }));
                    tlvParams.Add(new TlvItem(new byte[] { (byte)Hap_Tlv_Tags.ERROR_CODE },
                        new byte[] { (byte)Hap_Tlv_Errors.UNAVAILABLE }));

                    var c2 = new Tlv().Encode(tlvParams);
                    var length = c2.Length;
                    // Console.WriteLine($"length:{length}");
                    e.Response.ContentLength = length;
                    e.Response.ContentType = Const.PAIRING_RESPONSE_TYPE;
                    e.Response.Write(c2);
                    return;
                }

                Enum.TryParse(sequenceItem.Value[0].ToString(), out Hap_Tlv_States sequence);

                if (sequence == Hap_Tlv_States.M1)
                {
                    Console.WriteLine($"Pairing [1/5]");
                    Driver.SetupSrpVerifier();

                    tlvParams.Add(new TlvItem(new byte[] { (byte)Hap_Tlv_Tags.SEQUENCE_NUM },
                        new byte[] { (byte)Hap_Tlv_States.M2 }));
                    tlvParams.Add(new TlvItem(new byte[] { (byte)Hap_Tlv_Tags.SALT }, Driver.SrpServer.s));
                    tlvParams.Add(new TlvItem(new byte[] { (byte)Hap_Tlv_Tags.PUBLIC_KEY }, Driver.SrpServer.Bb));
                    var c2 = new Tlv().Encode(tlvParams);

                    e.Response.StatusCode = StatusCode.Status200OK;
                    var length = c2.Length;
                    // Console.WriteLine($"length:{length}");

                    e.Response.ContentLength = length;
                    e.Response.ContentType = Const.PAIRING_RESPONSE_TYPE;
                    e.Response.Headers["Server"] = "Kestrel";
                    // e.Response.Headers["Transfer-Encoding"] = "chunked";
                    e.Response.Headers["Date"] = DateTime.UtcNow.ToString("r");

                    e.Response.Write(c2);
                    e.Response.Send();
                }
                else if (sequence == Hap_Tlv_States.M3)
                {
                    Console.WriteLine($"Pairing [2/5]");
                    var A = result.FirstOrDefault(it => it.Tag[0] == (byte)Hap_Tlv_Tags.PUBLIC_KEY);
                    var M = result.FirstOrDefault(it => it.Tag[0] == (byte)Hap_Tlv_Tags.PASSWORD_PROOF);
                    Driver.SrpServer.SetA(A.Value);
                    var hamk = Driver.SrpServer.Verify(M.Value);
                    tlvParams.Add(new TlvItem(new byte[] { (byte)Hap_Tlv_Tags.SEQUENCE_NUM },
                        new byte[] { (byte)Hap_Tlv_States.M4 }));
                    tlvParams.Add(new TlvItem(new byte[] { (byte)Hap_Tlv_Tags.PASSWORD_PROOF }, hamk));
                    var c2 = new Tlv().Encode(tlvParams);

                    e.Response.StatusCode = StatusCode.Status200OK;
                    var length = c2.Length;
                    // Console.WriteLine($"length:{length}");
                    e.Response.ContentLength = length;
                    e.Response.ContentType = Const.PAIRING_RESPONSE_TYPE;
                    e.Response.Write(c2);
                }
                else if (sequence == Hap_Tlv_States.M5)
                {
                    Console.WriteLine($"Pairing [3/5]");
                    var encryptedData =
                        result.FirstOrDefault(it => it.Tag[0] == (byte)Hap_Tlv_Tags.ENCRYPTED_DATA);
                    var sessionKey = Driver.SrpServer.Kb;

                    var hkdf = HKDF.DeriveKey(new HashAlgorithmName(nameof(SHA512)), sessionKey, 32,
                        Const.PAIRING_3_SALT,
                        Const.PAIRING_3_INFO);

                    using var key = Key.Import(AeadAlgorithm.ChaCha20Poly1305, hkdf, KeyBlobFormat.RawSymmetricKey);
                    var decryptedData =
                        AeadAlgorithm.ChaCha20Poly1305.Decrypt(key, Const.PAIRING_3_NONCE, new byte[0],
                            encryptedData.Value);
                    var tlvItems = new Tlv().Decode(decryptedData);
                    var clientUserName = tlvItems.FirstOrDefault(it => it.Tag[0] == (byte)Hap_Tlv_Tags.USERNAME);
                    var clientLtpk = tlvItems.FirstOrDefault(it => it.Tag[0] == (byte)Hap_Tlv_Tags.PUBLIC_KEY);
                    var clientProof = tlvItems.FirstOrDefault(it => it.Tag[0] == (byte)Hap_Tlv_Tags.PROOF);

                    Console.WriteLine($"Pairing [4/5]");
                    var outputKey = HKDF.DeriveKey(new HashAlgorithmName(nameof(SHA512)), sessionKey, 32,
                        Const.PAIRING_4_SALT, Const.PAIRING_4_INFO);
                    var data = Utils.MergeBytes(outputKey, clientUserName.Value, clientLtpk.Value);
                    var ed25519 = SignatureAlgorithm.Ed25519;

                    var ed25519Key = PublicKey.Import(ed25519, clientLtpk.Value, KeyBlobFormat.RawPublicKey);
                    var ed25519VerifyResult = Ed25519.Ed25519.Verify(ed25519Key, data, clientProof.Value);
                    if (!ed25519VerifyResult)
                    {
                        //Bad signature, abort.
                    }

                    Console.WriteLine($"Pairing [5/5]");

                    var outputKey5 = HKDF.DeriveKey(new HashAlgorithmName(nameof(SHA512)), sessionKey, 32,
                        Const.PAIRING_5_SALT, Const.PAIRING_5_INFO);

                    var mac = Driver.State.Mac.ToBytes();
                    var serverPublic = Driver.State.PublicKey;
                    var material = Utils.MergeBytes(outputKey5, mac, serverPublic);
                    var primaryKey5 = Driver.State.PrivateKey;
                    var ed25519Key5 = Key.Import(ed25519, primaryKey5, KeyBlobFormat.RawPrivateKey);
                    var serverProof5 = Ed25519.Ed25519.Sign(ed25519Key5, material);
                    var tlvItems5 = new List<TlvItem>();
                    tlvItems5.Add(new TlvItem(new byte[] { (byte)Hap_Tlv_Tags.USERNAME }, mac));
                    tlvItems5.Add(new TlvItem(new byte[] { (byte)Hap_Tlv_Tags.PUBLIC_KEY }, serverPublic));
                    tlvItems5.Add(new TlvItem(new byte[] { (byte)Hap_Tlv_Tags.PROOF }, serverProof5));
                    var message5 = new Tlv().Encode(tlvItems5);
                    var decryptedData5 =
                        AeadAlgorithm.ChaCha20Poly1305.Encrypt(key, Const.PAIRING_5_NONCE, new byte[0], message5);

                    var clientUsernameStr = clientUserName.Value.GetString();
                    var clientGuid = new Guid(clientUsernameStr);

                    Driver.Pair(clientGuid, clientLtpk.Value, new byte[] { (byte)Hap_Permissions.ADMIN }, e.ConnectionString);

                    var tlvItem6 = new List<TlvItem>();
                    tlvItem6.Add(new TlvItem(new byte[] { (byte)Hap_Tlv_Tags.SEQUENCE_NUM },
                        new byte[] { (byte)Hap_Tlv_States.M6 }));
                    tlvItem6.Add(new TlvItem(new byte[] { (byte)Hap_Tlv_Tags.ENCRYPTED_DATA }, decryptedData5));
                    var message6 = new Tlv().Encode(tlvItem6);
                    e.Response.StatusCode = StatusCode.Status200OK;
                    var length = message6.Length;
                    // Console.WriteLine($"length:{length}");
                    e.Response.ContentLength = length;
                    e.Response.ContentType = Const.PAIRING_RESPONSE_TYPE;
                    e.Response.Write(message6);
                }
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }

    public async Task GetAccessories(HttpContext e)
    {
        Console.WriteLine("GetAccessories");
        var accessories = Driver.GetAccessories();

        var jsonSetting = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        var currentJson = JsonConvert.SerializeObject(accessories, jsonSetting);

        var bytes = currentJson.ToBytes();
        e.Response.HapCrypto = HapCrypto;
        e.Response.ContentLength = bytes.Length;
        e.Response.ContentType = Const.JSON_RESPONSE_TYPE;
        e.Response.Write(bytes);
        Console.WriteLine("GetAccessories 结束");
    }

    public async Task GetCharacteristics(HttpContext e)
    {
        Console.WriteLine($"{e.ConnectionString} GetCharacteristics");
        Const.Flag = true;
        var query = e.Request.Query["id"];
        var topics = query.Split(",").ToList();

        var items = new List<GetCharacteristicsResponseItem>();
        foreach (var topic in topics)
        {
            logger.LogInformation($"调试{topic}");
            var topicArr = topic.Split(".");
            var aid = int.Parse(topicArr[0]);
            var iid = int.Parse(topicArr[1]);
            var item = new GetCharacteristicsResponseItem()
            {
                Aid = aid,
                Iid = iid,
                Status = HapServerStatus.SERVICE_COMMUNICATION_FAILURE
            };
            try
            {
                Characteristics characteristics = null;
                var available = false;
                if (aid == Const.STANDALONE_AID)
                {
                    characteristics = Driver.Accessory.IidManager.GetObject(iid) as Characteristics;
                    available = true;
                }
                else
                {
                    var accessorys = ((Bridge)Driver.Accessory).Accessories;
                    if (accessorys.ContainsKey(aid))
                    {
                        var accessroy = accessorys[aid];
                        characteristics = accessroy.IidManager.GetObject(iid) as Characteristics;
                        available = true;
                    }
                    else
                    {
                        continue;
                    }
                }


                if (available)
                {
                    // if (topic == "1.9")
                    // {
                    //    characteristics.SetValue(21);
                    //     
                    // }
                    // if (topic == "1.7")
                    // {
                    //     characteristics.SetValue("hzp homeKit");
                    // }

                    item.Value = characteristics.GetValue();
                    item.Status = HapServerStatus.SUCCESS;
                }

                logger.LogInformation($"获取的特征信息为:{characteristics.ToString()}");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }

            items.Add(item);
        }

        if (items.Any(it => it.Status != HapServerStatus.SUCCESS))
        {
            e.Response.StatusCode = StatusCode.MULTI_STATUS;
            Console.WriteLine("GetCharacteristics 中间结果MULTI_STATUS");
        }
        else
        {
            e.Response.StatusCode = StatusCode.Status200OK;
            foreach (var item in items)
            {
                item.Status = null;
            }
        }

        var result = new GetCharacteristicsResponse()
        {
            Characteristics = items
        };

        var jsonSetting = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        var currentJson = JsonConvert.SerializeObject(result, jsonSetting);

        var bytes = currentJson.ToBytes();
        e.Response.HapCrypto = HapCrypto;
        e.Response.ContentLength = bytes.Length;
        e.Response.ContentType = Const.JSON_RESPONSE_TYPE;
        e.Response.Write(bytes);
        Console.WriteLine("GetCharacteristics结束");
    }

    /// <summary>
    /// Called from ``hapHandler`` when iOS configures the characteristics.在iOS客户端配置特性时调用
    /// </summary>
    /// <param name="e"></param>
    public async Task SetCharacteristics(HttpContext e)
    {
        Console.WriteLine($"{e.ConnectionString} SetCharacteristics");
        using (var stream = new MemoryStream())
        {
            await e.Request.Body.CopyToAsync(stream);
            stream.Seek(0, SeekOrigin.Begin);
            var characteristicsServerResponseStr = stream.ToArray().GetString();
            //{"characteristics":[{"aid":1,"iid":9,"ev":true}]}
            var characteristics =
                JsonConvert.DeserializeObject<CharacteristicsServerResponse>(characteristicsServerResponseStr);

            var updates = new Dictionary<int, Dictionary<int, object>>();
            //设置结果
            var setterResults = new Dictionary<int, Dictionary<int, HapServerStatus>>();
            var hadError = false;

            if (characteristics != null && characteristics.Characteristics?.Count > 0)
            {
                foreach (var item in characteristics.Characteristics)
                {
                    setterResults[item.Aid] = new Dictionary<int, HapServerStatus>();

                    if (item.Ev.HasValue)
                    {
                        var topic = Const.GetTopic(item.Aid, item.Iid);
                        var action = item.Ev == true ? "Subscribed" : "Unsubscribed";
                        Console.WriteLine($"{action} client {e.ConnectionString} to topic {topic}");
                        await Driver.SubscribeClientTopic(e.ConnectionString, topic, true);
                    }

                    if (item.Value == null)
                    {
                        continue;
                    }

                    updates[item.Aid] = new Dictionary<int, object>() { { item.Iid, item.Value } };
                }

                foreach (var pair in updates)
                {
                    var aid = pair.Key;
                    Accessory accessory;

                    if (Driver.Accessory.Aid == aid)
                    {
                        accessory = Driver.Accessory;
                    }
                    else
                    {
                        accessory = ((Bridge)Driver.Accessory).Accessories[aid];
                    }

                    var updatesByService = new Dictionary<Service, Characteristics>();
                    var charToIid = new Dictionary<string, sbyte>();
                    foreach (var keyValuePair in pair.Value)
                    {
                        var iid = keyValuePair.Key;
                        var characteristic = accessory.GetAccessoryCharacteristic(aid, iid);
                        var setResult = WrapCharSetter(characteristic, keyValuePair.Value, e.ConnectionString);
                        if (setResult != HapServerStatus.SUCCESS)
                        {
                            hadError = true;
                        }

                        setterResults[aid][iid] = setResult;
                    }
                }
            }

            if (!hadError)
            {
                Console.WriteLine("SetCharacteristics  nocontent");
                e.Response.HapCrypto = HapCrypto;
                e.Response.StatusCode = StatusCode.NO_CONTENT;
                //
                return;
            }

            var result = new SetCharacteristicsResult()
            {
                Characteristics = new List<SetCharacteristicsResultItem>()
            };

            var jsonSetting = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            var currentJson = JsonConvert.SerializeObject(result, jsonSetting);

            var bytes = currentJson.ToBytes();
            e.Response.HapCrypto = HapCrypto;
            e.Response.StatusCode = StatusCode.MULTI_STATUS;
            e.Response.ContentLength = bytes.Length;
            e.Response.ContentType = Const.JSON_RESPONSE_TYPE;
            Console.WriteLine("SetCharacteristics 结束前");
            e.Response.Write(bytes);
            Console.WriteLine("SetCharacteristics 结束");
        }
    }

    private HapServerStatus WrapCharSetter(Characteristics characteristics, object value, string connectionString)
    {
        try
        {
            characteristics.ClientUpdateValue(value, connectionString);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return HapServerStatus.SERVICE_COMMUNICATION_FAILURE;
        }

        return HapServerStatus.SUCCESS;
    }

    /// <summary>
    /// Handles arbitrary step of the pair verify process.Pair verify is session negotiation;处理配对验证过程的任意步骤。配对验证是会话协商。
    /// </summary>
    /// <param name="e"></param>
    public async Task PairVerify(HttpContext e)
    {
        try
        {
            Console.WriteLine("pair-verify");

            if (!Driver.State.IsPaired)
            {
                await SendAuthenticationErrorTlvResponse(e, Hap_Tlv_States.M2);
                return;
            }

            var c = e.Request;
            using (var stream = new MemoryStream())
            {
                await e.Request.Body.CopyToAsync(stream);
                stream.Seek(0, SeekOrigin.Begin);
                var result = new Tlv().Decode(stream.ToArray());
                var sequenceItem = result.FirstOrDefault(it => it.Tag[0] == 6);
                var tlvParams = new List<TlvItem>();

                if (sequenceItem == null || sequenceItem.Value.Length == 0)
                {
                    await SendAuthenticationErrorTlvResponse(e, Hap_Tlv_States.M2);
                    return;
                }

                Enum.TryParse(sequenceItem.Value[0].ToString(), out Hap_Tlv_States sequence);

                if (sequence == Hap_Tlv_States.M1)
                {
                    Console.WriteLine($"Pair verify [1/2]");

                    var otherPublicKeyBytes = result.FirstOrDefault(it =>
                        it.Tag.CompareTwoBytes(new byte[] { (byte)Hap_Tlv_Tags.PUBLIC_KEY }));

                    var x25519KeyPair = Utils.GenerateX25519KeyPair();
                    var sharedSecret =
                        Utils.ExchangeX25519PrivateKeyToSharedSecret(x25519KeyPair.PrivateKey,
                            otherPublicKeyBytes.Value);
                    //
                    var mac = Driver.State.Mac.ToBytes();
                    //  
                    var material = Utils.MergeBytes(x25519KeyPair.PublicKey, mac, otherPublicKeyBytes.Value);
                    //  
                    var primaryKey5 = Driver.State.PrivateKey;
                    var ed25519Key5 = Key.Import(Ed25519.Ed25519, primaryKey5, KeyBlobFormat.RawPrivateKey);
                    var serverProof5 = Ed25519.Ed25519.Sign(ed25519Key5, material);

                    var hkdf = HKDF.DeriveKey(new HashAlgorithmName(nameof(SHA512)), sharedSecret, 32,
                        Const.PVERIFY_1_SALT,
                        Const.PVERIFY_1_INFO);

                    PairVerifyOneEncryptionContext = new PairVerifyOneEncryptionContext()
                    {
                        ClientPublic = otherPublicKeyBytes.Value,
                        PrivateKey = x25519KeyPair.PrivateKey,
                        PublicKey = x25519KeyPair.PublicKey,
                        SharedKey = sharedSecret,
                        PreSessionKey = hkdf
                    };

                    var tlvItem5s = new List<TlvItem>();
                    tlvItem5s.Add(new TlvItem(new byte[] { (byte)Hap_Tlv_Tags.USERNAME }, mac));
                    tlvItem5s.Add(new TlvItem(new byte[] { (byte)Hap_Tlv_Tags.PROOF }, serverProof5));
                    var message5 = new Tlv().Encode(tlvItem5s);

                    using var k = Key.Import(AeadAlgorithm.ChaCha20Poly1305, hkdf, KeyBlobFormat.RawSymmetricKey);
                    var decryptedData5 =
                        AeadAlgorithm.ChaCha20Poly1305.Encrypt(k, Const.PVERIFY_1_NONCE, new byte[0], message5);

                    var tlvItem6 = new List<TlvItem>();
                    tlvItem6.Add(new TlvItem(new byte[] { (byte)Hap_Tlv_Tags.SEQUENCE_NUM },
                        new byte[] { (byte)Hap_Tlv_States.M2 }));
                    tlvItem6.Add(new TlvItem(new byte[] { (byte)Hap_Tlv_Tags.ENCRYPTED_DATA }, decryptedData5));
                    tlvItem6.Add(new TlvItem(new byte[] { (byte)Hap_Tlv_Tags.PUBLIC_KEY }, x25519KeyPair.PublicKey));
                    var message6 = new Tlv().Encode(tlvItem6);

                    e.Response.StatusCode = StatusCode.Status200OK;
                    var length = message6.Length;
                    e.Response.ContentLength = length;
                    e.Response.ContentType = Const.PAIRING_RESPONSE_TYPE;
                    e.Response.Write(message6);
                }
                else if (sequence == Hap_Tlv_States.M3)
                {
                    Console.WriteLine($"Pair verify [2/2]");
                    var encryptedDataBytes =
                        result.FirstOrDefault(it => it.Tag[0] == (byte)Hap_Tlv_Tags.ENCRYPTED_DATA);
                    var hkdf = PairVerifyOneEncryptionContext.PreSessionKey;
                    using var k = Key.Import(AeadAlgorithm.ChaCha20Poly1305, hkdf, KeyBlobFormat.RawSymmetricKey);
                    byte[] decryptedData;
                    try
                    {
                        decryptedData = AeadAlgorithm.ChaCha20Poly1305.Decrypt(k, Const.PVERIFY_2_NONCE,
                            new byte[0],
                            encryptedDataBytes.Value);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                        throw;
                    }

                    if (decryptedData == null || decryptedData.Length == 0)
                    {
                        var sdfdsf = 1;
                    }

                    var tlvItems = new Tlv().Decode(decryptedData);
                    var clientUserNameItem =
                        tlvItems.FirstOrDefault(it => it.Tag[0] == (byte)Hap_Tlv_Tags.USERNAME);
                    var clientProof = tlvItems.FirstOrDefault(it => it.Tag[0] == (byte)Hap_Tlv_Tags.PROOF);

                    var clientUuid = new Guid(clientUserNameItem.Value.GetString());

                    var clientPublic = PairVerifyOneEncryptionContext.ClientPublic;
                    var publicKey = PairVerifyOneEncryptionContext.PublicKey;


                    var material = Utils.MergeBytes(clientPublic, clientUserNameItem.Value, publicKey);
                    if (!Driver.State.PairedClients.ContainsKey(clientUuid))
                    {
                        await SendAuthenticationErrorTlvResponse(e, Hap_Tlv_States.M4);
                        return;
                    }
                    var permClientPublic = Driver.State.PairedClients[clientUuid];
                    var ed25519Key =
                        PublicKey.Import(Ed25519.Ed25519, permClientPublic, KeyBlobFormat.RawPublicKey);
                    var ed25519VerifyResult = Ed25519.Ed25519.Verify(ed25519Key, material, clientProof.Value);
                    Console.WriteLine($"{e.ConnectionString} Pair verify [2/2] verify result:{ed25519VerifyResult}");

                    tlvParams.Add(new TlvItem(new byte[] { (byte)Hap_Tlv_Tags.SEQUENCE_NUM },
                        new byte[] { (byte)Hap_Tlv_States.M4 }));
                    var c2 = new Tlv().Encode(tlvParams);

                    e.Response.StatusCode = StatusCode.Status200OK;
                    var length = c2.Length;

                    HapCrypto = new HapCrypto(PairVerifyOneEncryptionContext.SharedKey);
                    // Console.WriteLine($"length:{length}");
                    e.Response.ContentLength = length;
                    e.Response.ContentType = Const.PAIRING_RESPONSE_TYPE;
                    e.Response.Write(c2);
                }
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }

    public async Task Pairings(HttpContext e)
    {
        try
        {
            Console.WriteLine("Pairings");
            var c = e.Request;
            using (var stream = new MemoryStream())
            {
                await e.Request.Body.CopyToAsync(stream);
                stream.Seek(0, SeekOrigin.Begin);
                var result = new Tlv().Decode(stream.ToArray());
                var sequenceItem = result.FirstOrDefault(it => it.Tag[0] == 0);
                switch (sequenceItem.Value[0])
                {
                    case 3:
                        Console.WriteLine("AddPairing");
                        await AddPairing(result, e);
                        return;
                        break;
                    case 4:
                        Console.WriteLine("RemovePairing");
                        await RemovePairing(result, e);
                        return;
                        break;
                    case 5:
                        Console.WriteLine("ListPairings");
                        await ListPairings(result, e);
                        return;
                        break;
                }
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }

    private async Task AddPairing(List<TlvItem> tlvItems, HttpContext e)
    {
        var tlvParams = new List<TlvItem>();
        var clientUserNameItem =
            tlvItems.FirstOrDefault(it => it.Tag[0] == (byte)Hap_Tlv_Tags.USERNAME);
        var clientUuid = new Guid(clientUserNameItem.Value.GetString());
        var clientPublic = tlvItems.FirstOrDefault(it => it.Tag[0] == (byte)Hap_Tlv_Tags.PUBLIC_KEY);
        var permissions = tlvItems.FirstOrDefault(it => it.Tag[0] == (byte)Hap_Tlv_Tags.PERMISSIONS);
        Driver.Pair(clientUuid, clientPublic.Value, permissions.Value, e.ConnectionString);
        tlvParams.Add(new TlvItem(new byte[] { (byte)Hap_Tlv_Tags.SEQUENCE_NUM },
            new byte[] { (byte)Hap_Tlv_States.M2 }));
        var c2 = new Tlv().Encode(tlvParams);
        e.Response.StatusCode = StatusCode.Status200OK;
        e.Response.HapCrypto = HapCrypto;
        var length = c2.Length;
        e.Response.ContentLength = length;
        e.Response.ContentType = Const.PAIRING_RESPONSE_TYPE;
        e.Response.Write(c2);
    }

    private async Task RemovePairing(List<TlvItem> tlvItems, HttpContext e)
    {
        var tlvParams = new List<TlvItem>();
        var clientUserNameItem =
            tlvItems.FirstOrDefault(it => it.Tag[0] == (byte)Hap_Tlv_Tags.USERNAME);
        var clientUuid = new Guid(clientUserNameItem.Value.GetString());
        Driver.UnPair(clientUuid);
        tlvParams.Add(new TlvItem(new byte[] { (byte)Hap_Tlv_Tags.SEQUENCE_NUM },
            new byte[] { (byte)Hap_Tlv_States.M2 }));
        var c2 = new Tlv().Encode(tlvParams);
        e.Response.StatusCode = StatusCode.Status200OK;
        var length = c2.Length;
        e.Response.HapCrypto = HapCrypto;
        e.Response.ContentLength = length;
        e.Response.ContentType = Const.PAIRING_RESPONSE_TYPE;
        e.Response.Write(c2);
    }

    private async Task ListPairings(List<TlvItem> tlvItems, HttpContext e)
    {
        var tlvParams = new List<TlvItem>();
        var clientUserNameItem =
            tlvItems.FirstOrDefault(it => it.Tag[0] == (byte)Hap_Tlv_Tags.USERNAME);
        if (clientUserNameItem != null)
        {
            var clientUuid = new Guid(clientUserNameItem.Value.GetString());
            Driver.UnPair(clientUuid);
        }
       
        tlvParams.Add(new TlvItem(new byte[] { (byte)Hap_Tlv_Tags.SEQUENCE_NUM },
            new byte[] { (byte)Hap_Tlv_States.M2 }));
        var c2 = new Tlv().Encode(tlvParams);
        e.Response.StatusCode = StatusCode.Status200OK;
        var length = c2.Length;
        e.Response.ContentLength = length;
        e.Response.HapCrypto = HapCrypto;
        e.Response.ContentType = Const.PAIRING_RESPONSE_TYPE;
        e.Response.Write(c2);
    }
}