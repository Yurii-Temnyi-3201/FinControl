using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using MyFinControl.Data;
namespace MyFinControl
{
    public partial class AuthWindow : Window
    {
        // Налаштування користувача (сюди зберігаємо баланс і хеш пароля)
        private readonly UserSettings _userSettings;

        // Порожній конструктор потрібен для дизайнера/XAML
        public AuthWindow()
            : this(new UserSettings())
        {
        }

        // Основний конструктор: вікно отримує готовий UserSettings
        public AuthWindow(UserSettings settings)
        {
            InitializeComponent();
            _userSettings = settings ?? throw new ArgumentNullException(nameof(settings));
            InitializeMode();
        }

        /// <summary>
        /// Вибір режиму: показувати реєстрацію чи вхід.
        /// </summary>
        private void InitializeMode()
        {
            bool hasUserInDb;

            using (var db = new FinContext())
            {
                hasUserInDb = db.Users.Any();
            }

            if (!hasUserInDb)
            {
                // Пароль ще НЕ заданий – перший запуск реєстрація
                Title = "Реєстрація в системі";

                this.Width = 460;
                this.Height = 420;

                RegistrationPanel.Visibility = Visibility.Visible;
                LoginPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                // РЕЖИМ ВХОДУ (користувач уже є в БД)
                Title = "Вхід у систему";

                this.Width = 420;
                this.Height = 260;

                RegistrationPanel.Visibility = Visibility.Collapsed;
                LoginPanel.Visibility = Visibility.Visible;

                LoginPasswordBox.Focus();
            }
        }




        // Кнопка "Продовжити до входу" (реєстрація)
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegErrorText.Text = string.Empty;

            string pass = RegPasswordBox.Password;
            string confirm = RegConfirmPasswordBox.Password;

            // Перевірка пароля
            if (!PasswordPolicy.Validate(pass, out var passError))
            {
                RegErrorText.Text = passError;
                return;
            }

            if (pass != confirm)
            {
                RegErrorText.Text = "Паролі не співпадають.";
                return;
            }

            // Початкові баланси (тільки в UserSettings, в БД не пишемо)
            if (!decimal.TryParse(RegBalanceUAHTextBox.Text, out decimal balUAH))
            {
                RegErrorText.Text = "Невірний формат суми для UAH.";
                return;
            }
            if (!decimal.TryParse(RegBalanceUSDTextBox.Text, out decimal balUSD))
            {
                RegErrorText.Text = "Невірний формат суми для USD.";
                return;
            }
            if (!decimal.TryParse(RegBalanceEURTextBox.Text, out decimal balEUR))
            {
                RegErrorText.Text = "Невірний формат суми для EUR.";
                return;
            }
            if (balUAH < 0 || balUSD < 0 || balEUR < 0)
            {
                RegErrorText.Text = "Початкові суми не можуть бути від’ємними.";
                return;
            }

            // Записуємо в _userSettings
            _userSettings.BalanceUAH = balUAH;
            _userSettings.BalanceUSD = balUSD;
            _userSettings.BalanceEUR = balEUR;

            using (var db = new FinContext())
            {
                var result = PasswordHelper.CreateHash(pass);

                var user = db.Users.FirstOrDefault();
                if (user == null)
                {
                    user = new AppUser
                    {
                        Login = "admin",
                        PasswordHash = result.Hash,
                        Salt = result.Salt,
                        GoalUAH = 0m,
                        GoalUSD = 0m,
                        GoalEUR = 0m
                    };
                    db.Users.Add(user);
                }
                else
                {
                    user.Login = "admin";
                    user.PasswordHash = result.Hash;
                    user.Salt = result.Salt;
                    // цілі лишаємо
                }

                db.SaveChanges();
            }

            // Після реєстрації вікно входу
            RegistrationPanel.Visibility = Visibility.Collapsed;
            LoginPanel.Visibility = Visibility.Visible;

            this.Width = 420;
            this.Height = 260;

            RegPasswordBox.Password = string.Empty;
            RegConfirmPasswordBox.Password = string.Empty;
            LoginPasswordBox.Focus();
        }


        // Кнопка "Увійти"
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginErrorText.Text = string.Empty;

            string inputPass = LoginPasswordBox.Password;
            if (string.IsNullOrWhiteSpace(inputPass))
            {
                LoginErrorText.Text = "Введіть пароль.";
                return;
            }

            using (var db = new FinContext())
            {
                var user = db.Users.FirstOrDefault();
                if (user == null)
                {
                    LoginErrorText.Text = "Користувач не знайдений. Спочатку виконайте реєстрацію.";
                    return;
                }

                bool ok = PasswordHelper.VerifyPassword(inputPass, user.PasswordHash, user.Salt);
                if (!ok)
                {
                    LoginErrorText.Text = "Введено неправильний пароль. Спробуйте ще.";
                    return;
                }

                // ПІДХОПЛЮЄМО ЦІЛІ
                _userSettings.GoalUAH = user.GoalUAH;
                _userSettings.GoalUSD = user.GoalUSD;
                _userSettings.GoalEUR = user.GoalEUR;
            }

            var main = new MainWindow(_userSettings);
            Application.Current.MainWindow = main;
            main.Show();
            this.Close();
        }



    }
}
