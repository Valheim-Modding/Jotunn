using System.Security.Cryptography;
using System.Text;

namespace Jotunn.Utils
{
    /// <summary>
    ///     A util class for computing various hashes
    /// </summary>
    public static class HashUtils
    {
        /// <summary>
        ///     Compute a SHA256 hash from a given string
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public static string ComputeSha256Hash(string rawData)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));

            var stringBuilder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                stringBuilder.Append(bytes[i].ToString("x2"));
            }
            return stringBuilder.ToString();
        }
    }
}
