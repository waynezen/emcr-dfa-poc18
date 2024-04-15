
using System;
using System.Security.Cryptography;
using System.Text;

namespace pdfservice.Utils
{
    /// <summary>
    /// Helper methods for working with file hashes (md5, sha256, etc).
    /// </summary>
    public static class HashUtility
    {
        public static string GetMD5(byte[] input)
        {
            using (var md5 = MD5.Create())
            {
                return GetHash(md5, input);
            }
        }

        public static string GetSHA256(byte[] input)
        {
            using (var sha256 = SHA256.Create())
            {
                return GetHash(sha256, input);
            }
        }

        private static string GetHash(HashAlgorithm hashAlgorithm, byte[] input)
        {
            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(input);

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
    }
}