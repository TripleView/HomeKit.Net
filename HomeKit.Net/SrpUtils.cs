// using System.Security.Cryptography;
// using System.Text;
// using Org.BouncyCastle.Crypto.Agreement.Srp;
// using Org.BouncyCastle.Crypto.Digests;
// using Org.BouncyCastle.Crypto.Parameters;
// using Org.BouncyCastle.Math;
// using Org.BouncyCastle.Security;
// using SecureRemotePassword;
//
// namespace HomeKitSharp.HomeKit;
//
// public class SrpUtils
// {
//     public GetChallengeResult GetChallenge(byte[] pinCode,byte[]? salt=null,BigInteger? bValue=null)
//     {
//         var g = new BigInteger(Const.SrpGStr, 16);
//         var n= new BigInteger(Const.SrpNStr, 16);
//       
//         var srpParam= new Srp6GroupParameters(n,g);
//         var generator= new Srp6VerifierGenerator();
//         generator.Init(srpParam,new Sha512Digest());
//         var identityBytes = Encoding.UTF8.GetBytes("Pair-Setup");
//         salt =salt ?? GetSalt();
//         //Multiplier parameter
//        var k=  Srp6Utilities.CalculateK(new Sha512Digest(), n, g);
//        
//        // a user enters his name and password
//        // var client = new SrpClient();
//        // // var salt1 = Encoding.ASCII.GetString(salt);
//        // var salt1= SrpInteger.FromByteArray(salt);
//        // var privateKey1 = client.DerivePrivateKey(salt1, "Pair-Setup", Encoding.UTF8.GetString(pinCode));
//        // var verifier = client.DeriveVerifier(privateKey1);
//        
//        var privateKey=  Srp6Utilities.CalculateX(new Sha512Digest(),n,salt,identityBytes,pinCode);
//        //Password verifier (long)
//         var v= generator.GenerateVerifier(salt,identityBytes,pinCode);
//
//         // var A = new BigInteger(
//         //     "5736590827799008926049307398177727137216738827921788996766229103214925722798787747159540781765728316142131469964821972663282161336330047334592182716801249079676382575163019290677169826811050552159257226408567898339326599315849857321049636261824063827892959453437857595194985154082466082633565227589041563504183475558099708531569173103909634573124184557894873725392011982346358975011909136944524934347958601843794853223626422936312780364797994754230255118535969830154688651544375917189026616296499609361309325488182212163554995241551927384947014802590158213432513826485703521559479482070233880397482281994405706467965805836899159565863723967830190669578657765902422654324248246755160523014543722427977613188373508947806831882863045337220040026177815321367905773931224289770447998122003771143742656089414323419733642681854151497629890451230615037134333064455930266248171708832974284259386953218825009525912499630133615460783337");
//
//         var A = new BigInteger(
//             "1578564707429113913635673250483604268981823588842708576795028024857920930755854329978847076814333207910143467107594042834759126762747749025065087483081974209874953083164868483653595384476426612061403265544779590223112108703725745979899137693030007017486066884109090853615142287646314051466670024884060442626820477984440008078174480788109591892440232031883479826667317775200032485396366565023576563757716868822025038636155391816092011182186334540280634322976404993609388306967432369641674556849398327605879604868673279307979000859344127297863322211021100382800199561915041651560466250748555817521774401214392716858209294421324577065465878264919119420379867479141106501684259624197468588274453223489005344086715500572235667706875850352088548772082783453767512599622520280343718745488810341625984761555705767991286763585044371341076223795336379975445949342039580694066458443614560183035146078240000910974870753148207192986118694");
//
//         
//         var M = new BigInteger("6038634043602660746627733884937907889362853458086712838765199420098936290756108395031318044886246388286561464858250017688559377552260418519171151343139035");
//         var random = new SecureRandom();
//         var b= bValue?? GetSecretEphemeralValue();
//         
//         // var cf = new Srp6Client();
//         // cf.Init(srpParam,new Sha512Digest(),random);
//         // var dfdf= cf.GenerateClientCredentials(salt, identityBytes, pinCode);
//         
//         var B = (k.Multiply(v).Add( (g.ModPow(b, n)))).Mod(n);
//         
//         var u= Srp6Utilities.CalculateU(new Sha512Digest(), n, A, B);
//
//         var tt= Srp6Utilities.ValidatePublicValue(n, B);
//         
//         var cc= new MySrp6Server(b);
//         cc.Init(n,g,v,new Sha512Digest(),random);
//         var pubB= cc.GenerateServerCredentials();
//         var S = cc.CalculateSecret(A);
//
//         var sha512 = SHA512.Create();
//         var fsdfsdf= sha512.ComputeHash(S.ToByteArrayUnsigned());
//         var K= new BigInteger(fsdfsdf);
//
//         var m= CalculateM(identityBytes, salt, A.ToByteArrayUnsigned(), B.ToByteArrayUnsigned(), K.ToByteArrayUnsigned());
//         var dd = CompareTwoBytes( m,  M.ToByteArrayUnsigned());
//         // Srp6Utilities.CalculateM1(new Sha512Digest(), n, A, B, S)
//         
//         BigInteger computedM1 = Srp6Utilities.CalculateM1(new Sha512Digest(), n, A, pubB, S);
//         // var sdfsdfsdfsdfsdf= cc.CalculateServerEvidenceMessage();
//         var dsfsdfsdf= cc.VerifyClientEvidenceMessage(M);
//         
//             // var cc2 = cc.CalculateSecret(A);
//             // var cc1= cc.CalculateServerEvidenceMessage();
//             // cc.VerifyClientEvidenceMessage(M);
//         
//         var result = new GetChallengeResult()
//         {
//             Salt = salt,
//             B = B
//         };
//         
//         return result;
//     }
//
//     public byte[] CalculateM(byte[] userName,byte[] s,byte[] A,byte[]B,byte[]k)
//     {
//         var sha512 = SHA512.Create();
//         var g = new BigInteger(Const.SrpGStr, 16);
//         var n= new BigInteger(Const.SrpNStr, 16);
//         var gb= g.ToByteArrayUnsigned();
//         var nb = n.ToByteArrayUnsigned();
//         var hN= sha512.ComputeHash(nb);
//
//       
//         
//         var hG = sha512.ComputeHash(gb);
//         
//         
//        
//         
//         var hGroup = new List<byte>();
//         for (int i = 0; i < hN.Length; i++)
//         {
//             var b = (byte)(hN[i] ^ hG[i]);
//             hGroup.Add(b);
//         }
//         
//         var hU=sha512.ComputeHash(userName);
//         
//         hGroup.AddRange(hU);
//         hGroup.AddRange(s);
//         hGroup.AddRange(A);
//         hGroup.AddRange(B);
//         hGroup.AddRange(k);
//         
//         var hGTemp = new byte[]
//         {
//             179,214,62,246,170,251,46,151,150,218,224,6,254,96,242,14,83,38,254,30,28,82,165,2,30,178,23,71,202,51,223,234,143,242,3,189,69,162,84,67,149,215,61,25,137,156,23,74,104,60,158,120,50,150,221,22,147,235,199,28,245,165,61,163,205,63,168,144,172,137,26,169,169,210,51,70,241,228,246,152,176,18,64,173,129,12,240,41,213,213,127,199,159,27,31,196,226,196,8,98,223,179,130,142,210,197,138,168,15,103,138,69,167,244,215,139,156,74,195,63,220,85,70,242,161,4,48,179,88,147,4,241,79,164,16,225,34,244,180,71,196,199,54,36,69,143,51,60,15,22,15,36,190,75,190,62,75,85,25,68,252,233,188,4,133,34,252,98,15,2,218,50,233,169,40,113,3,23,240,76,25,133,88,19,122,49,25,78,40,53,149,249,6,74,153,80,91,248,122,99,149,186,169,212,92,190,151,4,51,104,154,112,40,231,156,166,159,236,53,23,48,23,112,249,48,116,242,32,224,174,32,144,223,127,40,40,218,83,2,236,154,90,134,0,225,48,46,62,174,158,120,78,90,154,118,11,226,27,131,150,115,2,75,226,197,73,89,63,213,136,172,85,82,63,134,149,72,7,111,104,215,40,138,106,135,25,104,202,36,59,24,190,132,80,49,134,209,104,127,107,216,147,23,180,140,0,10,132,127,115,190,16,240,195,36,144,141,3,220,76,35,153,150,32,228,50,60,100,153,168,90,251,239,215,21,191,117,181,27,236,140,252,130,82,223,174,162,208,48,247,239,31,183,189,254,153,4,231,171,15,248,177,24,36,181,239,38,119,82,130,2,233,127,171,177,54,89,23,152,39,48,188,176,166,136,39,119,22,201,187,166,64,184,233,137,57,235,63,23,43,192,92,56,147,120,156,217,255,6,62,33,123,92,24,48,90,16,190,188,17,127,158,246,206,103,13,62,238,59,234,168,10,22,216,184,125,11,244,134,218,137,132,224,162,64,197,81,90,64,176,1,202,191,209,242,200,149,22,75,143,56,243,73,118,149,224,157,135,123,39,45,27,78,233,123,45,213,204,246,5,127,191,29,171,162,207,15,174,177,69,180,108,177,214,2,83,252,22,120,30,152,115,116,50,54,38,47,163,98,171,227,208,88,163,56,26,231,49,185,104,159,105,184,101,81,252,106,38,118,150,126,82,84,28,187,207,0,158,200,105,218,229,81,83,83,192,207,211,199,232,206,249,153,146,2,183,210,119,146,175,29,59,250,87,71,49,80,154,64,125,140,18,78,127,4,162,195,170,238,146,46,190,193,173,220,132,130,164,53,182,216,3,99,168,198,104,132,152,205,166,95,177,247,68,249,251,47,248,88,53,142,30,197,41,232,174,233,32,222,62,186,120,236,112,113,88,226,230,140,210,195,140,229,223,248,90,133,52,222,112,253,104,38,229,34,109,166,45,204,28,242,145,131,51,80,30,201,73,174,154,97,27,176,202,119,75,80,55,192,170,47,168,203,171,200,247,56,6,66,253,234,93,115,109,119,4,84,102,241,49,242,185,90,168,170,168,172,115,208,235,68,186,127,170,81,237,249,188,220,76,71,241,123,132,129,96,165,16,42,114,236,89,189,89,11,218,103,68,107,243,159,96,217,182,9,201,6,52,221,226,157,152,116,234,4,31,69,184,85,29,202,93,219,150,39,137,251,188,17,18,198,194,212,26,114,17,185,199,34,81,121,250,180,117,193,14,109,46,10,144,87,180,193,53,118,20,106,107,24,155,99,137,148,108,113,163,153,14,191,61,233,83,204,17,153,21,73,9,234,166,177,230,245,92,44,100,137,235,53,215,129,190,0,48,199,76,254,120,23,44,35,103,117,198,235,15,99,59,79,85,75,252,210,250,156,95,160,76,3,238,109,253,37,162,34,36,206,78,82,34,229,98,6,32,237,49,135,38,103,128,95,129,66,82,221,221,29,36,95,200,5,125,157,6,224,153,210,222,204,142,77,154,50,238,134,180,44,3,211,43,101,210,98,213,239,153,93,123,65,70,251,51,110,118,137,50,188,176,46,49,141,117,127,155,220,15,85,177,105,166,211,114,29,2,92,237,31,20,212,89,30,7,223,210,103,117,165,64,10,221,138,246,227,115,212,249,18,52,185,230,149,8,21,175,65,88,248,113,147,59,49,196,19,0,145,251
//         };
//      
//         
//         var result=sha512.ComputeHash(hGroup.ToArray());
//         return new System.Numerics.BigInteger(result, true, true).ToByteArray(true, true);
//     }
//
//     private bool CompareTwoBytes(byte[] b1, byte[] b2)
//     {
//         if (b1.Length != b2.Length)
//         {
//             return false;
//         }
//
//         for (int i = 0; i < b1.Length; i++)
//         {
//             if (b1[i] != b2[i])
//             {
//                 return false;
//             }
//         }
//
//         return true;
//     }
//     
//     /// <summary>
//     /// get Secret ephemeral value
//     /// </summary>
//     /// <returns></returns>
//     public BigInteger GetSecretEphemeralValue()
//     {
//         var randon = new Random();
//         var result = new byte[32];
//         randon.NextBytes(result);
//         var temp= new BigInteger(result);
//         return temp;
//     }
//     
//     private byte[] GetSalt()
//     {
//         var randon = new Random();
//         var result = new byte[16];
//         randon.NextBytes(result);
//         return result;
//         // for (int i = 0; i < 16; i++)
//         // {
//         //    
//         // }
//     }
// }
//
// public class GetChallengeResult
// {
//     public byte[] Salt { get; set; }
//     public BigInteger B { get; set; }
// }
//
// public class MySrp6Server : Srp6Server
// {
//     private BigInteger primaryKey;
//
//     public MySrp6Server(BigInteger primaryKey)
//     {
//         this.primaryKey = primaryKey;
//     }
//
//     protected override BigInteger SelectPrivateValue()
//     {
//         return primaryKey;
//     }
// }
//
// public class MySrp6Client : Srp6Client
// {
//     private BigInteger primaryKey;
//
//     public MySrp6Client(BigInteger primaryKey)
//     {
//         this.primaryKey = primaryKey;
//     }
//
//     
//     
//     protected override BigInteger SelectPrivateValue()
//     {
//         return primaryKey;
//     }
//     
// }
//   