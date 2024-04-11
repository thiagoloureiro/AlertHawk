using System.Security.Cryptography;

namespace AlertHawk.Notification.Tests
{
    public static class Utils
    {
        public static (string, string) GenerateAesKeyAndIv()
        {
            using Aes aes = Aes.Create();
            aes.GenerateKey();
            aes.GenerateIV();
            return (ByteArrayToHexString(aes.Key), ByteArrayToHexString(aes.IV));
        }

        public static string ByteArrayToHexString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }
}