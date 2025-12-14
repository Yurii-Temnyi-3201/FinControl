using System;
using System.Windows;

namespace MyFinControl
{
    public partial class AddCategoryWindow : Window
    {
        public string CategoryName {get; private set;}
        public string CategoryEmoji {get; private set;}
        public OperationType CategoryType {get; private set;}

        private readonly bool _isEditMode;
        private readonly Category _category;
        // РЕЖИМ ДОДАТИ НОВУ КАТЕГОРІЮ
        public AddCategoryWindow()
        {   InitializeComponent();
            _isEditMode = false;
            Title = "Додавання категорії";
            // За замовчуванням – витрата
            if (ExpenseRadio != null) ExpenseRadio.IsChecked = true;
        }
        // РЕЖИМ РЕДАГУВАННЯ ІСНУЮЧОЇ КАТЕГОРІЇ
        public AddCategoryWindow(Category categoryParam)
        {    InitializeComponent();
            _isEditMode = true;
            _category = categoryParam ?? throw new ArgumentNullException("categoryParam");
                Title = "Редагування категорії";
        // Заповнюємо існуючими значеннями
            NameTextBox.Text = categoryParam.Name;
            EmojiTextBox.Text = categoryParam.Emoji;
        // Виставляємо тип у радіокнопках
          if (categoryParam.Type == OperationType.Income)
        { if (IncomeRadio != null) IncomeRadio.IsChecked = true;
          if (ExpenseRadio != null) ExpenseRadio.IsChecked = false;
        } else
        { if (ExpenseRadio != null) ExpenseRadio.IsChecked = true;
          if (IncomeRadio != null) IncomeRadio.IsChecked = false;
        }
        }
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var name = NameTextBox.Text?.Trim();
            var emoji = EmojiTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            { MessageBox.Show("Введіть назву категорії.", "Помилка",
              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(emoji))
                emoji = "🛜";
            // 1 Тип категорії з радіокнопок
            bool isIncome = (IncomeRadio != null && IncomeRadio.IsChecked == true);
            CategoryType = isIncome ? OperationType.Income : OperationType.Expense;
            // 2 Значення, які забирає MainWindow
            CategoryName = name;
            CategoryEmoji = emoji;
            // 3 Якщо редагуємо оновлюємо об'єкт категорії
            if (_isEditMode && _category != null)
            {   _category.Name = name;
                _category.Emoji = emoji;
                _category.Type = CategoryType;
            } DialogResult = true;
              Close();
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {DialogResult = false;
         Close();
        }
    }
}
