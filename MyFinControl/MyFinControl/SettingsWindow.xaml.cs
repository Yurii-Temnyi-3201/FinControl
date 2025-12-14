using System;
using System.Linq;
using System.Windows;
using MyFinControl.Data;

namespace MyFinControl
{
    public partial class SettingsWindow : Window
    {
        private readonly UserSettings _userSettings;

        public SettingsWindow(UserSettings settings)
        {
            InitializeComponent();
            _userSettings = settings;
        }

        // Показати помилку внизу вікна
        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Скидаємо попередню помилку
            ErrorText.Visibility = Visibility.Collapsed;
            ErrorText.Text = string.Empty;

            string oldPass = OldPasswordBox.Password;
            string newPass = NewPasswordBox.Password;
            string confirm = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(oldPass))
            {
                ShowError("Введіть поточний пароль.");
                return;
            }

            using (var db = new FinContext())
            {
                var user = db.Users.FirstOrDefault();

                if (user == null)
                {
                    ShowError("Користувача не знайдено. Спочатку виконайте реєстрацію.");
                    return;
                }

                bool oldOk = PasswordHelper.VerifyPassword(oldPass, user.PasswordHash, user.Salt);
                if (!oldOk)
                {
                    ShowError("Поточний пароль введено неправильно.");
                    return;
                }

                if (!PasswordPolicy.Validate(newPass, out var passError))
                {
                    ShowError(passError);
                    return;
                }

                if (newPass != confirm)
                {
                    ShowError("Новий пароль і підтвердження не співпадають.");
                    return;
                }

                var result = PasswordHelper.CreateHash(newPass);
                user.PasswordHash = result.Hash;
                user.Salt = result.Salt;

                db.SaveChanges();

                if (_userSettings != null)
                {
                    _userSettings.PasswordHash = user.PasswordHash;
                }
            }

            MessageBox.Show(
                "Пароль успішно змінено.",
                "Налаштування",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            //  ДОДАНО: чистимо поля
            OldPasswordBox.Password = string.Empty;
            NewPasswordBox.Password = string.Empty;
            ConfirmPasswordBox.Password = string.Empty;

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
