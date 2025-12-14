using System.Windows;
using MyFinControl.Data;
namespace MyFinControl
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // 1. Створюємо налаштування користувача
            // Поки що новий об'єкт. Потім сюди додаси завантаження з БД/файлу.
            var userSettings = new UserSettings();

            // 2. Створюємо вікно авторизації
            var authWindow = new AuthWindow(userSettings);

            // Робимо його головним вікном додатку на старті
            this.MainWindow = authWindow;

            // 3. Показуємо вікно
            authWindow.Show();
        }

protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Створює файл UserData.db + таблиці Categories і Operations
        using (var db = new FinContext())
        {
            db.Database.EnsureCreated();
        }
    }

}
}
