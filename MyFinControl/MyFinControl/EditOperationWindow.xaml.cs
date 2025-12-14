using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MyFinControl
{
    public partial class EditOperationWindow : Window
    {
        private readonly IList<Category> _categories;

        private readonly int? _savingsCategoryId; // ✅ ДОДАНО: стабільне визначення "Накопичення"

        /// <summary>
        /// Операція, яку редагуємо / створюємо.
        /// </summary>
        public Operation EditableOperation { get; }

        /// <summary>
        /// Якщо для операції "Накопичення" користувач ввів ціль – тут буде значення.
        /// Інакше null.
        /// </summary>
        public decimal? SavingsGoal { get; private set; }

        // ✅ ВИПРАВЛЕНО: приймаємо savingsCategoryId
        public EditOperationWindow(Operation operation, IList<Category> categories, int? savingsCategoryId)
        {
            InitializeComponent();

            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (categories == null) throw new ArgumentNullException(nameof(categories));

            EditableOperation = operation;
            _categories = categories;
            _savingsCategoryId = savingsCategoryId; // ✅

            // --- Категорії ---
            CategoryComboBox.ItemsSource = _categories;

            var currentCategory = _categories.FirstOrDefault(c => c.Id == operation.CategoryId);
            if (currentCategory != null)
                CategoryComboBox.SelectedItem = currentCategory;
            else if (_categories.Count > 0)
                CategoryComboBox.SelectedIndex = 0;

            // Опис
            DescriptionTextBox.Text = operation.Description ?? string.Empty;

            // Сума (записуємо модуль)
            if (operation.Amount == 0m)
                AmountTextBox.Text = string.Empty;
            else
                AmountTextBox.Text = Math.Abs(operation.Amount)
                    .ToString("F2", CultureInfo.InvariantCulture);

            // Валюта
            foreach (ComboBoxItem item in CurrencyComboBox.Items)
            {
                if (item.Content != null && item.Content.ToString() == operation.Currency)
                {
                    CurrencyComboBox.SelectedItem = item;
                    break;
                }
            }

            // ✅ викликаємо логіку підлаштування
            CategoryComboBox_SelectionChanged(CategoryComboBox, null);
        }

        // ✅ Конструктор для дизайнера
        public EditOperationWindow()
            : this(new Operation
            {
                Amount = 0m,
                Currency = "UAH",
                CreatedAt = DateTime.Now
            }, new List<Category>(), null) // ✅
        {
        }

        // ----------------- ДОПОМІЖНЕ -----------------

        // ✅ ДОДАНО: визначення чи це "Накопичення" по Id (не по назві)
        private bool IsSavingsCategory(Category selected)
        {
            if (selected == null) return false;
            if (!_savingsCategoryId.HasValue) return false;
            return selected.Id == _savingsCategoryId.Value;
        }

        // ----------------- ОБРОБНИКИ -----------------

        /// <summary>
        /// Міняємо вигляд вікна, якщо вибрана категорія "Накопичення":
        /// показуємо SavingsPanel і фіксуємо тип операції за Category.Type.
        /// </summary>
        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = CategoryComboBox.SelectedItem as Category;

            bool isSavings = IsSavingsCategory(selected); // ✅ ВИПРАВЛЕНО

            // Показати/сховати панель накопичень
            if (SavingsPanel != null)
                SavingsPanel.Visibility = isSavings ? Visibility.Visible : Visibility.Collapsed;

            // Фіксуємо тип операції за типом категорії
            if (IncomeRadio != null && ExpenseRadio != null && selected != null)
            {
                if (selected.Type == OperationType.Expense)
                {
                    ExpenseRadio.IsChecked = true;

                    IncomeRadio.IsChecked = false;
                }
                else
                {
                    IncomeRadio.IsChecked = true;

                    ExpenseRadio.IsChecked = false;
                }

                // ✅ ВИПРАВЛЕНО: якщо ти хочеш, щоб тип завжди був фіксований — блокуємо обидві
                // (це повторює твою попередню логіку, але без дублювання коду)
                ExpenseRadio.IsEnabled = false;
                IncomeRadio.IsEnabled = false;
            }

            // Якщо пішли з "Накопичення" – очищаємо поле цілі
            if (!isSavings && GoalTextBox != null)
            {
                GoalTextBox.Text = string.Empty;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            // --- Категорія ---
            var selectedCategory = CategoryComboBox.SelectedItem as Category;
            if (selectedCategory == null)
            {
                MessageBox.Show("Оберіть категорію.", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool isSavingsCategory = IsSavingsCategory(selectedCategory); // ✅ ВИПРАВЛЕНО

            // --- Сума ---
            if (!decimal.TryParse(
                    AmountTextBox.Text.Replace(',', '.'),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Введіть коректну суму більше 0.", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // --- Валюта ---
            var currencyItem = CurrencyComboBox.SelectedItem as ComboBoxItem;
            if (currencyItem == null)
            {
                MessageBox.Show("Оберіть валюту.", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string currency = currencyItem.Content != null
                ? currencyItem.Content.ToString()
                : "UAH";

            // --- Знак суми ---
            decimal signedAmount;

            if (isSavingsCategory)
            {
                // Накопичення: гроші йдуть ІЗ поточного балансу у "скарбничку".
                // Для балансу це мінус, у прогресі накопичення потім береш Math.Abs().
                signedAmount = -amount;
            }
            else
            {
                // Для інших категорій знак визначається типом категорії
                bool isIncome = (selectedCategory.Type == OperationType.Income);
                signedAmount = isIncome ? amount : -amount;
            }

            // --- Якщо це "Накопичення" — читаємо ціль із GoalTextBox ---
            SavingsGoal = null;
            if (isSavingsCategory && GoalTextBox != null)
            {
                string goalText = (GoalTextBox.Text ?? "").Trim();
                if (goalText.Length > 0)
                {
                    if (!decimal.TryParse(goalText.Replace(',', '.'),
                                          NumberStyles.Any,
                                          CultureInfo.InvariantCulture,
                                          out decimal goal) || goal <= 0)
                    {
                        MessageBox.Show("Введіть коректну суму цілі (більше 0).",
                            "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    SavingsGoal = goal;
                }
            }

            // --- Оновлюємо об’єкт операції ---
            EditableOperation.CategoryId = selectedCategory.Id;
            EditableOperation.CategoryName = selectedCategory.Name;
            EditableOperation.Description = (DescriptionTextBox.Text ?? string.Empty).Trim();
            EditableOperation.Amount = signedAmount;
            EditableOperation.Currency = currency;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
