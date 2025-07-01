using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace AlertHawk.Authentication.Infrastructure.Helpers;

public static class PasswordHasher
{
    private const int Iterations = 150000;
    private const int KeySize = 32;

    public static string GenerateRandomPassword(int length)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Password length must be positive.");

        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()-_=+";
        var password = new StringBuilder();
        using (var rng = RandomNumberGenerator.Create())
        {
            byte[] uintBuffer = new byte[4];

            while (password.Length < length)
            {
                rng.GetBytes(uintBuffer);
                uint num = BitConverter.ToUInt32(uintBuffer, 0);
                long index = num % chars.Length;
                password.Append(chars[(int)index]);
            }
        }

        return password.ToString();
    }

    public static string GenerateSalt()
    {
        var salt = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return Convert.ToBase64String(salt);
    }

    public static string HashPassword(string password, string salt)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var saltBytes = Convert.FromBase64String(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(passwordBytes, saltBytes, Iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(KeySize);
        return Convert.ToBase64String(hash);
    }

    public static bool VerifyPassword(string inputPassword, string storedHash, string storedSalt)
    {
        var saltBytes = Convert.FromBase64String(storedSalt);
        var storedHashBytes = Convert.FromBase64String(storedHash);
        var hashToCompare =
            Rfc2898DeriveBytes.Pbkdf2(inputPassword, saltBytes, Iterations, HashAlgorithmName.SHA256, KeySize);

        return CryptographicOperations.FixedTimeEquals(hashToCompare, storedHashBytes);
    }
}