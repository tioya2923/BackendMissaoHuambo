using BCrypt.Net;

namespace MissaoBackend.Services;

public static class PasswordHasher
{
    public static string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password);

    public static bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);

    public static bool IsHashed(string value) =>
        value.StartsWith("$2a$") || value.StartsWith("$2b$") || value.StartsWith("$2y$");
}
