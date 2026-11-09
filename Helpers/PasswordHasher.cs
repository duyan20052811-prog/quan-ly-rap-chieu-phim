using System.Security.Cryptography;
using System.Text;

namespace CinemaManagement.Helpers;

public static class PasswordHasher
{
    // SHA-256 dạng HEX viết hoa (64 ký tự) - khớp với cột password_hash NVARCHAR(64)
    public static string Hash(string password) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password ?? "")));

    public static bool Verify(string password, string hash) =>
        string.Equals(Hash(password), hash, StringComparison.OrdinalIgnoreCase);
}
