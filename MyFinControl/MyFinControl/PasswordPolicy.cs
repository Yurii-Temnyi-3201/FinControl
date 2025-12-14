using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyFinControl
{
    public static class PasswordPolicy
    {
        public const int MinLength = 6;
        public const int MaxLength = 20;

        public static bool Validate(string password, out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrEmpty(password))
            {
                errorMessage = "Пароль не може бути порожнім.";
                return false;
            }

            if (password.Length < MinLength)
            {
                errorMessage = $"Пароль має містити щонайменше {MinLength} символів.";
                return false;
            }

            if (password.Length > MaxLength)
            {
                errorMessage = $"Пароль не може бути довшим за {MaxLength} символів.";
                return false;
            }

            // Забороняємо будь-які пробіли (у т.ч. таби/переноси)
            if (password.Any(char.IsWhiteSpace))
            {
                errorMessage = "Пароль не може містити пробіли.";
                return false;
            }

            if (!password.Any(char.IsLetter))
            {
                errorMessage = "Пароль має містити хоча б одну літеру.";
                return false;
            }

            if (!password.Any(char.IsDigit))
            {
                errorMessage = "Пароль має містити хоча б одну цифру.";
                return false;
            }

            return true;
        }
    }
}
