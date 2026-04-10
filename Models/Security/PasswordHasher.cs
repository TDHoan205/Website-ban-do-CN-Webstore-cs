using System.Security.Cryptography;
using System.Text;

namespace Webstore.Models.Security
{
    public static class PasswordHasher
    {
        public static string HashPassword(string password, string salt)
        {
            using var sha = SHA256.Create();
            var combined = Encoding.UTF8.GetBytes(password + ":" + salt);
            var hash = sha.ComputeHash(combined);
            return Convert.ToHexString(hash);
        }

        public static string GenerateSalt(int size = 16)
        {
            var bytes = RandomNumberGenerator.GetBytes(size);
            return Convert.ToHexString(bytes);
        }

        public static bool Verify(string password, string salt, string expectedHash)
        {
            var actual = HashPassword(password, salt);
            return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(actual), Convert.FromHexString(expectedHash));
        }
    }
}

