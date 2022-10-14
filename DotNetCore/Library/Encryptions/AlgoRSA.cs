using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace DotNetCore.Library.Encryptions
{
    public class RsaSecretKey
    {
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
        public byte[] PubKey { get; set; }
        public byte[] PrvKey { get; set; }
    }

    public static class AlgoRsa
    {
        public static RsaSecretKey GenerateRSASecretKey(int keySize)
        {
            RSACryptoServiceProvider rsa = new(keySize);
            return new RsaSecretKey
            {
                PrivateKey = rsa.ToXmlString(true),
                PublicKey = rsa.ToXmlString(false),
                PubKey = rsa.ExportCspBlob(false),
                PrvKey = rsa.ExportCspBlob(true)
            };
        }

        public static string GeneratePublicKey(string xmlPrivateKey)
        {
            using RSACryptoServiceProvider rsa = new();
            rsa.FromXmlString(xmlPrivateKey);
            return rsa.ToXmlString(false);
        }

        public static string RSAEncrypt(string xmlPublicKey, string content)
        {
            string encryptedContent = string.Empty;
            using (RSACryptoServiceProvider rsa = new())
            {
                rsa.FromXmlString(xmlPublicKey);
                byte[] encryptedData = rsa.Encrypt(Encoding.Default.GetBytes(content), false);
                encryptedContent = Convert.ToBase64String(encryptedData);
            }
            return encryptedContent;
        }

        public static string RSADecrypt(string xmlPrivateKey, string content)
        {
            string decryptedContent = string.Empty;
            using (RSACryptoServiceProvider rsa = new())
            {
                rsa.FromXmlString(xmlPrivateKey);
                byte[] decryptedData = rsa.Decrypt(Convert.FromBase64String(content), false);
                decryptedContent = Encoding.UTF8.GetString(decryptedData);
            }
            return decryptedContent;
        }

        private static byte FromCharacterToByte(char character, int index, int shift = 0)
        {
            var value = (byte)character;
            if (0x40 < value && 0x47 > value || 0x60 < value && 0x67 > value)
            {
                if (0x40 == (0x40 & value))
                    if (0x20 == (0x20 & value))
                        value = (byte)((value + 0xA - 0x61) << shift);
                    else
                        value = (byte)((value + 0xA - 0x41) << shift);
            }
            else if (0x29 < value && 0x40 > value)
            {
                value = (byte)((value - 0x30) << shift);
            }
            else
            {
                throw new FormatException(string.Format(
                    "Character '{0}' at index '{1}' is not valid alphanumeric character.", character, index));
            }

            return value;
        }

        private static byte[] HexToByteArrayInternal(string value)
        {
            byte[] bytes = null;
            if (string.IsNullOrEmpty(value))
            {
                bytes = new byte[0];
            }
            else
            {
                var string_length = value.Length;
                var character_index = value.StartsWith("0x", StringComparison.Ordinal) ? 2 : 0;
                // Does the string define leading HEX indicator '0x'. Adjust starting index accordingly.               
                var number_of_characters = string_length - character_index;

                var add_leading_zero = false;
                if (0 != number_of_characters % 2)
                {
                    add_leading_zero = true;

                    number_of_characters += 1; // Leading '0' has been striped from the string presentation.
                }

                bytes = new byte[number_of_characters / 2]; // Initialize our byte array to hold the converted string.

                var write_index = 0;
                if (add_leading_zero)
                {
                    bytes[write_index++] = FromCharacterToByte(value[character_index], character_index);
                    character_index += 1;
                }

                for (var read_index = character_index; read_index < value.Length; read_index += 2)
                {
                    var upper = FromCharacterToByte(value[read_index], read_index, 4);
                    var lower = FromCharacterToByte(value[read_index + 1], read_index + 1);

                    bytes[write_index++] = (byte)(upper | lower);
                }
            }

            return bytes;
        }

        public static byte[] HexToByteArray(this string value)
        {
            try
            {
                return HexToByteArrayInternal(value);
            }
            catch (FormatException ex)
            {
                throw new FormatException(string.Format(
                    "String '{0}' could not be converted to byte array (not hex?).", value), ex);
            }
        }

        public static string ToHex(this byte[] value, bool prefix = false)
        {
            var strPrex = prefix ? "0x" : "";
            return strPrex + string.Concat(value.Select(b => b.ToString("x2")).ToArray());
        }

        public static (ECDsa privateKey, ECDsa publicKey) CreateKeys(ECCurve curve)
        {
            var ecCurve = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var privateKey = ecCurve.ExportParameters(true).GetPrvKey();
            var publicKey = ecCurve.ExportParameters(true).GetPubKey();

            var pp = privateKey.ToHex();
            var loadback = LoadECDsa(privateKey, publicKey);
            var r = Verify(loadback);

            
            var privateSigningKey = new ECDsaSecurityKey(ecCurve);
            var publicSigningKey = new ECDsaSecurityKey(ECDsa.Create(ecCurve.ExportParameters(false)));

            var token = CreateJwe(privateSigningKey, null);
            var result = DecryptAndValidateJwe(token, publicSigningKey, null);

                X509Certificate2 cert = new CertificateRequest("C=US,O=Microsoft,OU=WGA,CN=TedSt", ecCurve, HashAlgorithmName.SHA256)
                .CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(-2));
            var a = cert.GetECDsaPrivateKey;
            var b = cert.GetECDsaPublicKey;
            return (null, null);
        }

        public static byte[] GetPubKey(this ECParameters eCParameters)
        {
            return eCParameters.Q.X.Concat(eCParameters.Q.Y).ToArray();
        }

        public static byte[] GetPrvKey(this ECParameters eCParameters)
        {
            return eCParameters.D;
        }

        public static ECDsa LoadECDsa(string PrvKey) => LoadECDsa(PrvKey.HexToByteArray());

        public static ECDsa LoadECDsa(byte[] PrvKey) => ECDsa.Create(new ECParameters { Curve = ECCurve.NamedCurves.nistP256, D = PrvKey });

        public static ECDsa LoadECDsa(byte[] PrvKey, byte[] PubKey)
        {
            return ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                D = PrvKey,
                Q = new ECPoint
                {
                    X = PubKey.Take(32).ToArray(),
                    Y = PubKey.Skip(32).ToArray()
                }
            });
        }

        private static string CreateJwe(SecurityKey signingKey, SecurityKey encryptionKey)
        {
            var handler = new JsonWebTokenHandler();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Audience = "api1",
                Issuer = "https://idp.example.com",
                Claims = new Dictionary<string, object> { { "sub", "811e790749a24d8a8f766e1a44dca28a" } },

                // private key for signing
                SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.EcdsaSha256),

                // public key for encryption
                //EncryptingCredentials = new EncryptingCredentials(encryptionKey, SecurityAlgorithms.RsaOAEP, SecurityAlgorithms.Aes256CbcHmacSha512)
            };

            return handler.CreateToken(tokenDescriptor);
        }

        private static bool DecryptAndValidateJwe(string token, SecurityKey signingKey, SecurityKey encryptionKey)
        {
            var handler = new JsonWebTokenHandler();

            TokenValidationResult result = handler.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidAudience = "api1",
                    ValidIssuer = "https://idp.example.com",

                    // public key for signing
                    IssuerSigningKey = signingKey,

                    // private key for encryption
                    //TokenDecryptionKey = encryptionKey
                });

            return result.IsValid;
        }

        private static bool Verify(ECDsa key)
        {
            byte[] data = Encoding.UTF8.GetBytes("dooooooooooooom");
            // create signature
            var signature = key.SignData(data, HashAlgorithmName.SHA256);

            // validate signature with public key
            var pubKey = ECDsa.Create(key.ExportParameters(false));
            return pubKey.VerifyData(data, signature, HashAlgorithmName.SHA256);
        }

    }
}
