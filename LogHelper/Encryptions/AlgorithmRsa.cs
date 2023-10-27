using System.Security.Cryptography;
using System.Text;

namespace LogHelper.Encryptions
{
    public static class AlgorithmRsa
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
    }

    public class RsaSecretKey
    {
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
        public byte[] PubKey { get; set; }
        public byte[] PrvKey { get; set; }
    }
}
