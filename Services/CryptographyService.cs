using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BH.SDK.Services
{
    // NOT WHAT PROTECTS A LEVEL. Level protection - a password-protected package, and a level whose
    // document sits encrypted on disk - is OpenPGP, and it lives in Services/Crypto. The reason is
    // not strength but reach: the artifacts this project writes have to open in tools people already
    // have (gpg, and every archiver that reads what gpg wrote), and an AES blob of our own opens in
    // nothing. What stays here is a raw AES-256-CBC primitive for whatever inside the SDK needs one.
    //
    // It also has no integrity check, deliberately left as it is: adding a MAC here would make a
    // second half-format that still opens in nothing, which is exactly the direction this class is
    // no longer taking. A caller that needs to tell a wrong password from a damaged file wants
    // PgpSymmetricService, whose MDC answers that question.

    /// <summary> Raw AES-256-CBC over bytes, keyed by a password through PBKDF2-SHA256. </summary>
    public class CryptographyService
    {
        public enum Algorithm
        {
            None = 0,
            AES = 1,
        }

        public string Encrypt(string data, string password, Algorithm algorithm = Algorithm.AES)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            var passwordBytes = Encoding.UTF8.GetBytes(password);

            var result = Encrypt(bytes, passwordBytes, algorithm);
            return Convert.ToBase64String(result);
        }
        public string Decrypt(string data, string password, Algorithm algorithm = Algorithm.AES)
        {
            var bytes = Convert.FromBase64String(data);
            var passwordBytes = Encoding.UTF8.GetBytes(password);

            var result = Decrypt(bytes, passwordBytes, algorithm);
            return Encoding.UTF8.GetString(result);
        }
        public byte[] Encrypt(byte[] data, byte[] password, Algorithm algorithm = Algorithm.AES)
        {
            return algorithm switch
            {
                Algorithm.None => data,
                Algorithm.AES => EncryptAES(data, password),
                _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null)
            };
        }
        public byte[] Decrypt(byte[] data, byte[] password, Algorithm algorithm)
        {
            return algorithm switch
            {
                Algorithm.None => data,
                Algorithm.AES => DecryptAES(data, password),
                _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null)
            };
        }
        
        // Game Encryption
        
        public const int KeySize = 32; // 256 bit
        public const int IvSize = 16; // 128 bit
        public const int SaltSize = 32; // 256 bit
        public const int Iterations = 100_000; // Rfc2898DeriveBytes iterations

        public byte[] EncryptAES(byte[] bytes, byte[] passwordBytes)
        {
            var salt = GetRandomBytes(SaltSize);
            var iv = GetRandomBytes(IvSize);
            
            using var keyDerivation = new Rfc2898DeriveBytes(passwordBytes, 
                salt, Iterations, HashAlgorithmName.SHA256);
            var key = keyDerivation.GetBytes(KeySize);
            
            using var aes = CreateAES();
            using var encryptor = aes.CreateEncryptor(key, iv);
            
            using var memoryStream = new MemoryStream();
            memoryStream.Write(salt, 0, salt.Length);
            memoryStream.Write(iv, 0, iv.Length);
            
            using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            cryptoStream.Write(bytes, 0, bytes.Length);
            cryptoStream.FlushFinalBlock();
            
            return memoryStream.ToArray();
        }
        public byte[] DecryptAES(byte[] bytes, byte[] passwordBytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            // The guard is not defensive tidiness: without it a short input computes a NEGATIVE
            // array length below and throws OverflowException from inside the crypto path, which
            // reads as "the cipher broke" rather than "this file is not one of ours". Anything
            // shorter than the header alone cannot be ciphertext this class wrote.
            if (bytes.Length < SaltSize + IvSize)
                throw new ArgumentException(
                    $"Ciphertext is {bytes.Length} bytes, shorter than the {SaltSize + IvSize} byte header.",
                    nameof(bytes));

            var salt = new byte[SaltSize];
            var iv = new byte[IvSize];
            var cipherBytes = new byte[bytes.Length - SaltSize - IvSize];
            
            // take salt and iv from beginning
            Buffer.BlockCopy(bytes, 0, salt, 0, SaltSize);
            Buffer.BlockCopy(bytes, SaltSize, iv, 0, IvSize);
            Buffer.BlockCopy(bytes, SaltSize + IvSize, cipherBytes, 0, cipherBytes.Length);
            
            using var keyDerivation = new Rfc2898DeriveBytes(passwordBytes, 
                salt, Iterations, HashAlgorithmName.SHA256);
            var key = keyDerivation.GetBytes(KeySize);
            
            using var aes = CreateAES();
            using var decryptor = aes.CreateDecryptor(key, iv);
            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Write);
            
            cryptoStream.Write(cipherBytes, 0, cipherBytes.Length);
            cryptoStream.FlushFinalBlock();
            
            return memoryStream.ToArray();
        }

        private static Aes CreateAES()
        {
            var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            return aes;
        }

        // System.Random, seeded with a constant, is what this used to be - so every installation
        // produced the SAME salt and the SAME IV, in the same order. Under AES-CBC that makes the
        // same password over the same plaintext yield byte-identical ciphertext, and the IV was
        // readable by anyone who opened this repository, which is everyone: the SDK is open source.
        // Nothing here may draw key material from a PRNG that is not the platform's own.
        private static byte[] GetRandomBytes(int length)
        {
            var bytes = new byte[length];
            RandomNumberGenerator.Fill(bytes);
            return bytes;
        }
    }
}