﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MyMailClient
{
    static class Cryptography
    {
        public const string KEY_DELIVERY_HEADER = "x-MMS-key-delivery-x";
        public const string SIGNATURE_HEADER = "x-MSS-signature-x";
        public const string ENCRYPTION_ID_HEADER = "x-MSS-encryption-id-x";
        public const string SIGNATURE_ID_HEADER = "x-MSS-signature-id-x";
        private const bool DO_OAEP_PADDING = true;
        private const CipherMode AES_CIPHER_MODE = CipherMode.CBC;
        private const PaddingMode AES_PADDING_MODE = PaddingMode.ISO10126;
        public static readonly Encoding E = Encoding.Unicode;

        public static readonly Encoding Enc = Encoding.UTF8;
        public static byte[] GetSHA1(byte[] data)
        {
            byte[] hash;
            using (SHA1Managed sha1 = new SHA1Managed())
                hash = sha1.ComputeHash(data);
            return hash;
        }
        public static byte[] GetSHA1(string data)
        {
            return GetSHA1(Enc.GetBytes(data));
        }

        public static string Encrypt(string data, CryptoKey rsaPublicKey)
        {
            byte[] encryptedData, encryptedAesKey, aesIV;
            try
            {
                using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
                {
                    RSAParameters rsaParams;
                    using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                    {
                        rsa.FromXmlString(rsaPublicKey.PublicKey);
                        rsaParams = rsa.ExportParameters(false);
                    }

                    aes.Mode = AES_CIPHER_MODE;
                    aes.GenerateKey();
                    aes.GenerateIV();
                    encryptedData = AesEncrypt(data, aes.Key, aes.IV);
                    encryptedAesKey = RsaEncrypt(aes.Key, rsaParams, DO_OAEP_PADDING);
                    aesIV = aes.IV;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return new XDocument
            (
                new XElement
                (
                    "root",
                    new XElement("data", Convert.ToBase64String(encryptedData)),
                    new XElement("key", Convert.ToBase64String(encryptedAesKey)),
                    new XElement("IV", Convert.ToBase64String(aesIV))
                )
            ).ToString();
        }

        public static byte[] AesEncrypt(string plainText, byte[] key, byte[] IV)
        {
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (key == null || key.Length <= 0)
                throw new ArgumentNullException("key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;

            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                aes.Mode = AES_CIPHER_MODE;
                aes.Key = key;
                aes.IV = IV;
                aes.Padding = AES_PADDING_MODE;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt, E))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }
                    encrypted = msEncrypt.ToArray();
                }
            }
            return encrypted;
        }
        public static byte[] RsaEncrypt(byte[] dataToEncrypt, RSAParameters RSAKeyInfo, bool doOAEPPadding = DO_OAEP_PADDING)
        {
            try
            {
                byte[] encryptedData;
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.ImportParameters(RSAKeyInfo);
                    encryptedData = rsa.Encrypt(dataToEncrypt, doOAEPPadding);
                }
                return encryptedData;
            }
            catch (CryptographicException e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
    }
}
