using System;
using System.Security.Cryptography;
using System.Text;
using AstraSyndicate.Save;

namespace AstraSyndicate.Security
{
    /// <summary>
    /// Reference save protector: AES-256-CBC encryption with an HMAC-SHA256 tag (encrypt-then-MAC), so save files
    /// can be neither read nor edited without the key. Stands in until the dedicated encryption module is
    /// plugged in through its own SaveProtectorProvider.
    /// </summary>
    /// <remarks>
    /// Format (version 1): "ASV1" | 16-byte random IV | ciphertext | 32-byte HMAC over everything before it.
    /// Bytes without the "ASV1" header are treated as unencrypted legacy saves, which are accepted only when the
    /// protector was created with <c>acceptPlaintext</c> (development builds migrating old saves).
    /// </remarks>
    public sealed class AesHmacSaveProtector : ISaveDataProtector
    {
        private const int BlockSize = 16;
        private const int IvSize = 16;
        private const int MacSize = 32;
        private static readonly byte[] Header = { (byte)'A', (byte)'S', (byte)'V', (byte)'1' };

        private readonly byte[] _encryptionKey;
        private readonly byte[] _macKey;
        private readonly bool _acceptPlaintext;

        /// <param name="masterKey">At least 16 bytes of secret key material; separate encryption and MAC keys are derived from it.</param>
        /// <param name="acceptPlaintext">Accept unencrypted saves written before encryption was enabled.</param>
        public AesHmacSaveProtector(byte[] masterKey, bool acceptPlaintext = false)
        {
            if (masterKey == null || masterKey.Length < 16) throw new ArgumentException("A master key of at least 16 bytes is required.", nameof(masterKey));

            _encryptionKey = DeriveKey(masterKey, "astra-save-encryption");
            _macKey = DeriveKey(masterKey, "astra-save-authentication");
            _acceptPlaintext = acceptPlaintext;
        }

        public byte[] Protect(byte[] plainBytes)
        {
            if (plainBytes == null) throw new ArgumentNullException(nameof(plainBytes));

            var iv = new byte[IvSize];
            using (RandomNumberGenerator random = RandomNumberGenerator.Create())
            {
                random.GetBytes(iv);
            }

            byte[] cipher;
            using (Aes aes = CreateAes())
            using (ICryptoTransform encryptor = aes.CreateEncryptor(_encryptionKey, iv))
            {
                cipher = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            }

            var output = new byte[Header.Length + IvSize + cipher.Length + MacSize];
            Buffer.BlockCopy(Header, 0, output, 0, Header.Length);
            Buffer.BlockCopy(iv, 0, output, Header.Length, IvSize);
            Buffer.BlockCopy(cipher, 0, output, Header.Length + IvSize, cipher.Length);
            byte[] mac = ComputeMac(output, output.Length - MacSize);
            Buffer.BlockCopy(mac, 0, output, output.Length - MacSize, MacSize);
            return output;
        }

        public bool TryUnprotect(byte[] protectedBytes, out byte[] plainBytes)
        {
            plainBytes = null;
            if (protectedBytes == null) return false;

            if (!HasHeader(protectedBytes))
            {
                if (!_acceptPlaintext) return false;
                plainBytes = protectedBytes;
                return true;
            }

            int cipherLength = protectedBytes.Length - Header.Length - IvSize - MacSize;
            if (cipherLength <= 0 || cipherLength % BlockSize != 0) return false;

            // Authenticate before decrypting: tampered data never reaches the cipher.
            byte[] expectedMac = ComputeMac(protectedBytes, protectedBytes.Length - MacSize);
            if (!FixedTimeEquals(expectedMac, protectedBytes, protectedBytes.Length - MacSize)) return false;

            var iv = new byte[IvSize];
            Buffer.BlockCopy(protectedBytes, Header.Length, iv, 0, IvSize);
            try
            {
                using (Aes aes = CreateAes())
                using (ICryptoTransform decryptor = aes.CreateDecryptor(_encryptionKey, iv))
                {
                    plainBytes = decryptor.TransformFinalBlock(protectedBytes, Header.Length + IvSize, cipherLength);
                }
                return true;
            }
            catch (CryptographicException)
            {
                plainBytes = null;
                return false;
            }
        }

        private static Aes CreateAes()
        {
            Aes aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            return aes;
        }

        private byte[] ComputeMac(byte[] data, int count)
        {
            using (var hmac = new HMACSHA256(_macKey))
            {
                return hmac.ComputeHash(data, 0, count);
            }
        }

        private static byte[] DeriveKey(byte[] masterKey, string purpose)
        {
            using (var hmac = new HMACSHA256(masterKey))
            {
                return hmac.ComputeHash(Encoding.UTF8.GetBytes(purpose));
            }
        }

        private static bool HasHeader(byte[] data)
        {
            if (data.Length < Header.Length) return false;
            for (int i = 0; i < Header.Length; i++)
            {
                if (data[i] != Header[i]) return false;
            }
            return true;
        }

        // Compares without exiting early, so timing does not reveal how much of a forged tag was right.
        private static bool FixedTimeEquals(byte[] expected, byte[] data, int offset)
        {
            int difference = 0;
            for (int i = 0; i < expected.Length; i++) difference |= expected[i] ^ data[offset + i];
            return difference == 0;
        }
    }
}
