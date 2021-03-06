﻿using Newtonsoft.Json;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace AsymmetricEncryption
{
    class Program
    {
        static async Task Main()
        {
            try
            {
                // Generate a new key pair
                var (publicKey, publicPrivateKey) = GenerateKeyPair();
                // If you see the keys, uncomment the following two lines:
                // Console.WriteLine(JsonConvert.SerializeObject(publicKey));
                // Console.WriteLine(JsonConvert.SerializeObject(publicPrivateKey));

                // Ask our partner for the secret message. She will encrypt it with
                // our public key.
                var ciphertext = await GetEncryptedData(publicKey);

                // Use our private key to decrypt the secret message.
                var secretMessage = await GetDecryptedMessage(ciphertext, publicPrivateKey);
                Console.WriteLine(secretMessage);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error: {e.Message}");
            }
        }

        static (RSAParameters publicKey, RSAParameters publicPrivateKey) GenerateKeyPair()
        {
            // Create object implementing RSA. Note that this version of the
            // constructor generates a random 2048-bit key pair.
            using (var rsa = new RSACng())
            {
                return (publicKey: rsa.ExportParameters(false),
                    publicPrivateKey: rsa.ExportParameters(true));
            }
        }

        static async Task<(byte[] encryptedKey, byte[] encryptedIV, byte[] ciphertext)> GetEncryptedData(RSAParameters publicKey)
        {
            byte[] ciphertext;
            byte[] key;
            byte[] iv;

            // Use AES to encrypt secret message.
            using (var aes = new AesCng())
            {
                key = aes.Key;
                iv = aes.IV;
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        await swEncrypt.WriteAsync("Secret Message");
                    }

                    ciphertext = msEncrypt.ToArray();
                }
            }

            // Use RSA to encrypt AES key/IV
            using (var rsa = new RSACng())
            {
                // Import given public key to RSA
                rsa.ImportParameters(publicKey);

                // Encrypt symmetric key/IV using RSA (public key).
                // Return encrypted key/IV and ciphertext
                return (encryptedKey: rsa.Encrypt(key, RSAEncryptionPadding.OaepSHA512),
                    encryptedIV: rsa.Encrypt(iv, RSAEncryptionPadding.OaepSHA512),
                    ciphertext);
            }
        }

        static async Task<string> GetDecryptedMessage((byte[] encryptedKey, byte[] encryptedIV, byte[] ciphertext) encryptedMessage,
            RSAParameters publicPrivateKey)
        {
            using (var rsa = new RSACng())
            {
                // Import key pair
                rsa.ImportParameters(publicPrivateKey);

                // Decrypt key/IV using asymmetric RSA algorithm
                var key = rsa.Decrypt(encryptedMessage.encryptedKey, RSAEncryptionPadding.OaepSHA512);
                var iv = rsa.Decrypt(encryptedMessage.encryptedIV, RSAEncryptionPadding.OaepSHA512);

                // Use symmetric AES algorithm to decrypt secret message
                using (var aes = new AesCng() { Key = key, IV = iv })
                using (var msDecrypt = new MemoryStream(encryptedMessage.ciphertext))
                using (var csDecrypt = new CryptoStream(msDecrypt, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (var srDecrypt = new StreamReader(csDecrypt))
                {
                    return await srDecrypt.ReadToEndAsync();
                }
            }
        }
    }
}
