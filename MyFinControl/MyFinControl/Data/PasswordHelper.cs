using System;
using System.Security.Cryptography;

namespace MyFinControl.Data
{
    public static class PasswordHelper
    {
        public struct HashResult
        {
            public string Hash;
            public string Salt;
        }

        // Створення хешу + солі для нового паролю
        public static HashResult CreateHash(string password)
        {
            // 1. Генеруємо випадкову сіль (16 байт)
            byte[] saltBytes = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(saltBytes);
            }

            // 2. Отримуємо хеш через PBKDF2 (Rfc2898DeriveBytes)
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000))
            {
                byte[] hashBytes = pbkdf2.GetBytes(32); // 32 байти = 256 біт

                return new HashResult
                {
                    Hash = Convert.ToBase64String(hashBytes),
                    Salt = Convert.ToBase64String(saltBytes)
                };
            }
        }

        // Перевірка паролю
        public static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            byte[] saltBytes = Convert.FromBase64String(storedSalt);
            byte[] hashBytes = Convert.FromBase64String(storedHash);

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000))
            {
                byte[] newHash = pbkdf2.GetBytes(32);

                if (newHash.Length != hashBytes.Length)
                    return false;

                for (int i = 0; i < newHash.Length; i++)
                {
                    if (newHash[i] != hashBytes[i])
                        return false;
                }

                return true;
            }
        }
    }
}

