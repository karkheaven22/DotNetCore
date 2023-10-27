using System.Security.Cryptography;
using System.Text;

namespace LogHelper.Encryptions
{
    public static class AlgorithmECDsa
    {
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
            byte[] bytes;
            if (string.IsNullOrEmpty(value))
            {
                bytes = Array.Empty<byte>();
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

        public static byte[] GetPubKey(this ECParameters eCParameters)
        {
            return eCParameters.Q.X!.Concat(eCParameters.Q.Y!).ToArray();
        }

        public static byte[] GetPrvKey(this ECParameters eCParameters)
        {
            return eCParameters.D!;
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

        public static string GeneratePublicKey(string PrvKey, bool prefix = false)
        {
            ECDsa ecCurve = LoadPublicKey(PrvKey);
            return ecCurve.GeneratePublicKey(prefix);
        }

        public static string GeneratePublicKey(this ECDsa ecCurve, bool prefix = false)
        {
            var PublicKey = ecCurve.ExportParameters(true).GetPubKey();
            return PublicKey.ToHex(prefix);
        }

        public static string GeneratePrivateKey(bool prefix = false)
        {
            var ecCurve = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var privateKey = ecCurve.ExportParameters(true).GetPrvKey();
            return privateKey.ToHex(prefix);
        }

        public static byte[] SignData(string PrvKey, string message)
        {
            ECDsa ecCurve = LoadECDsa(PrvKey);
            return ecCurve.SignData(message);
        }

        public static byte[] SignData(this ECDsa PrvKey, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            return PrvKey.SignData(data, HashAlgorithmName.SHA256);
        }

        public static bool VerifyData(string PubKey, byte[] signature, string message)
        {
            ECDsa ecCurve = LoadECDsa(PubKey);
            return ecCurve.VerifyData(signature, message);
        }

        public static bool VerifyData(this ECDsa PubKey, byte[] signature, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            return PubKey.VerifyData(data, signature, HashAlgorithmName.SHA256);
        }

        public static ECDsa LoadPublicKey(string PubKey)
        {
            return LoadPublicKey(PubKey.HexToByteArray());
        }

        public static ECDsa LoadPublicKey(byte[] publicKeyBytes)
        {
            return ECDsa.Create(new ECParameters() { 
                Curve = ECCurve.NamedCurves.nistP256, 
                Q = new ECPoint
                {
                    X = publicKeyBytes.Take(32).ToArray(),
                    Y = publicKeyBytes.Skip(32).ToArray()
                }
            });
        }

        private static bool TestCase_SignVerify(ECDsa key)
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
