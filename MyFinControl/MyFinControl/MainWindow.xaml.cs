using Microsoft.EntityFrameworkCore;
using MyFinControl.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MyFinControl
{
    public partial class MainWindow : Window
    {
        // Колекції для прив'язки в XAML
        public ObservableCollection<Operation> Operations { get; } =
            new ObservableCollection<Operation>();

        public ObservableCollection<Category> ExpenseCategories { get; } =
            new ObservableCollection<Category>();

        public ObservableCollection<Category> IncomeCategories { get; } =
            new ObservableCollection<Category>();

        // Внутрішні списки
        private readonly List<Category> _allCategories = new List<Category>();
        private readonly List<Operation> _allOperations = new List<Operation>();

        private UserSettings _userSettings;
        private int _nextCategoryId = 1;
        private int _nextOperationId = 1;
        private int? _currentCategoryFilterId = null;

        private int? _savingsCategoryId = null; // ✅ ДОДАНО: щоб не залежати від назви "Накопичення"

        // --- Конструктори ---

        // Використовується при запуску після авторизації
        public MainWindow(UserSettings settings)
        {
            InitializeComponent();

            _userSettings = settings ?? new UserSettings
            {
                BalanceUAH = 0m,
                BalanceUSD = 0m,
                BalanceEUR = 0m
            };

            DataContext = this;

            // 1. Читаємо з БД
            LoadDataFromDatabase();

            // 2. Якщо категорій ще немає – ініціалізуємо
            if (_allCategories.Count == 0)
            {
                InitCategories();
                SaveCategoriesToDb();
                LoadDataFromDatabase(); // перечитуємо, щоб Id були саме з БД
            }

            // 3. Підганяємо лічильники Id під максимальні значення з БД
            RecalculateNextIds();

            // 4. Перераховуємо баланси з усіх операцій
            RecalculateBalancesFromOperations();

            // 5. Гарантуємо існування категорії "Накопичення"
            EnsureSavingsCategoryExists();

            // 6. Оновлюємо колекції для категорій
            RefreshCategoryCollections();

            UpdateCategoryTotals();
            ApplyFilter(null);
            UpdateHeaderInfo();
        }

        // Конструктор для дизайнера / запуску без авторизації
        public MainWindow()
            : this(new UserSettings
            {
                BalanceUAH = 0m,
                BalanceUSD = 0m,
                BalanceEUR = 0m
            })
        {
        }

        // ------------------ ЗАВАНТАЖЕННЯ / ЗБЕРЕЖЕННЯ ------------------

        private void LoadDataFromDatabase()
        {
            using (var db = new FinContext())
            {
                var cats = db.Categories.AsNoTracking().ToList();
                var ops = db.Operations.AsNoTracking().ToList();

                _allCategories.Clear();
                _allCategories.AddRange(cats);

                _allOperations.Clear();
                _allOperations.AddRange(ops);
            }
        }

        /// <summary>
        /// Після завантаження з БД перераховуємо лічильники Id,
        /// щоб не було двох об'єктів з однаковим Id.
        /// </summary>
        private void RecalculateNextIds()
        {
            _nextCategoryId = _allCategories.Any()
                ? _allCategories.Max(c => c.Id) + 1
                : 1;

            _nextOperationId = _allOperations.Any()
                ? _allOperations.Max(o => o.Id) + 1
                : 1;
        }

        private void SaveCategoriesToDb()
        {
            using (var db = new FinContext())
            {
                foreach (var c in _allCategories)
                {
                    if (!db.Categories.Any(x => x.Id == c.Id))
                        db.Categories.Add(c);
                }
                db.SaveChanges();
            }
        }

        // ✅ ДОДАНО: точкові методи для операцій (замість перезапису всієї таблиці)
        private void AddOperationToDb(Operation op)
        {
            using (var db = new FinContext())
            {
                db.Operations.Add(op);
                db.SaveChanges();
            }
        }

        private void UpdateOperationInDb(Operation op)
        {
            using (var db = new FinContext())
            {
                db.Operations.Update(op); // attach + update по ключу Id
                db.SaveChanges();
            }
        }

        private void DeleteOperationFromDb(int operationId)
        {
            using (var db = new FinContext())
            {
                var entity = db.Operations.FirstOrDefault(x => x.Id == operationId);
                if (entity != null)
                {
                    db.Operations.Remove(entity);
                    db.SaveChanges();
                }
            }
        }

        private void DeleteOperationsByCategoryFromDb(int categoryId)
        {
            using (var db = new FinContext())
            {
                var ops = db.Operations.Where(o => o.CategoryId == categoryId).ToList();
                if (ops.Count > 0)
                {
                    db.Operations.RemoveRange(ops);
                    db.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Зберегти цілі накопичення в таблицю Users.
        /// </summary>
        private void SaveGoalsToDb()
        {
            using (var db = new FinContext())
            {
                var user = db.Users.FirstOrDefault();
                if (user == null) return;

                user.GoalUAH = _userSettings.GoalUAH;
                user.GoalUSD = _userSettings.GoalUSD;
                user.GoalEUR = _userSettings.GoalEUR;

                db.SaveChanges();
            }
        }

        // ------------------ ІНІЦІАЛІЗАЦІЯ КАТЕГОРІЙ ------------------

        private void InitCategories()
        {
            // ПРИБУТКИ
            var mainIncome = new Category
            {
                Id = _nextCategoryId++,
                Name = "Основний дохід",
                Type = OperationType.Income,
                Emoji = "💰"
            };
            _allCategories.Add(mainIncome);

            var gifts = new Category
            {
                Id = _nextCategoryId++,
                Name = "Подарунки",
                Type = OperationType.Income,
                Emoji = "🎁"
            };
            _allCategories.Add(gifts);

            // ВИТРАТИ
            var food = new Category
            {
                Id = _nextCategoryId++,
                Name = "Їжа",
                Type = OperationType.Expense,
                Emoji = "🍔"
            };
            _allCategories.Add(food);

            var transport = new Category
            {
                Id = _nextCategoryId++,
                Name = "Транспорт",
                Type = OperationType.Expense,
                Emoji = "🚌"
            };
            _allCategories.Add(transport);

            var clothes = new Category
            {
                Id = _nextCategoryId++,
                Name = "Одяг",
                Type = OperationType.Expense,
                Emoji = "👕"
            };
            _allCategories.Add(clothes);

            // Накопичення (спецкатегорія)
            var savings = new Category
            {
                Id = _nextCategoryId++,
                Name = "Накопичення",
                Type = OperationType.Income,   // тип Income, але суму зберігаємо з мінусом
                Emoji = "🐷"
            };
            _allCategories.Add(savings);
        }

        private void EnsureSavingsCategoryExists()
        {
            var existing = _allCategories
                .FirstOrDefault(c => c.Name == "Накопичення" && c.Type == OperationType.Income);

            if (existing != null)
            {
                _savingsCategoryId = existing.Id; //  ДОДАНО збереження
                return;
            }

            var savings = new Category
            {
                Id = _nextCategoryId++, // щоб не змішувати авто-Id і ручні Id
                Name = "Накопичення",
                Type = OperationType.Income,
                Emoji = "🐷"
            };

            using (var db = new FinContext())
            {
                db.Categories.Add(savings);
                db.SaveChanges();
            }

            _allCategories.Add(savings);
            _savingsCategoryId = savings.Id; // ДОДАНО збереженяня
        }

        // ------------------ ДОПОМІЖНІ ------------------

        private string GetSelectedCurrency()
        {
            if (CurrencyComboBox != null &&
                CurrencyComboBox.SelectedItem is ComboBoxItem item &&
                item.Content is string text)
            {
                return text;
            }
            return "UAH";
        }

        private void ApplyFilter(int? categoryId)
        {
            _currentCategoryFilterId = categoryId;
            string cur = GetSelectedCurrency();
            Operations.Clear();

            IEnumerable<Operation> src = _allOperations.Where(o => o.Currency == cur);

            if (categoryId.HasValue)
                src = src.Where(o => o.CategoryId == categoryId.Value);

            foreach (var op in src.OrderByDescending(o => o.CreatedAt))
                Operations.Add(op);

            UpdateHeaderInfo();
        }

        private void UpdateCategoryTotals()
        {
            string cur = GetSelectedCurrency();
            foreach (var cat in _allCategories)
            {
                var sum = _allOperations
                     .Where(o => o.Currency == cur && o.CategoryId == cat.Id)
                     .Sum(o => o.Amount);

                // Для "Накопичення" показуємо модуль (без мінуса)
                if (_savingsCategoryId.HasValue && cat.Id == _savingsCategoryId.Value)
                    cat.TotalAmount = Math.Abs(sum);
                else
                    cat.TotalAmount = sum;
            }
        }

        private void RefreshCategoryCollections()
        {
            ExpenseCategories.Clear();
            IncomeCategories.Clear();

            foreach (var cat in _allCategories)
            {
                if (cat.Type == OperationType.Expense)
                    ExpenseCategories.Add(cat);
                else
                    IncomeCategories.Add(cat);
            }
        }

        /// <summary>
        /// Баланси не беремо з Users, а рахуємо з усіх операцій.
        /// </summary>
        private void RecalculateBalancesFromOperations()
        {
            _userSettings.BalanceUAH = _allOperations
                .Where(o => o.Currency == "UAH")
                .Sum(o => o.Amount);

            _userSettings.BalanceUSD = _allOperations
                .Where(o => o.Currency == "USD")
                .Sum(o => o.Amount);

            _userSettings.BalanceEUR = _allOperations
                .Where(o => o.Currency == "EUR")
                .Sum(o => o.Amount);
        }

        private void UpdateHeaderInfo()
        {
            if (CurrentBalanceText == null ||
                MonthIncomeText == null ||
                MonthExpenseText == null ||
                MonthOperationsCountText == null ||
                LastOperationText == null ||
                LastOperationDateText == null)
            {
                return;
            }

            string cur = GetSelectedCurrency();
            DateTime now = DateTime.Now;
            var culture = CultureInfo.CurrentCulture;

            // --------- БАЛАНС ---------
            decimal balance = _userSettings.GetBalanceForCurrency(cur);
            CurrentBalanceText.Text = cur + " " + balance.ToString("0.##", culture);

            // --------- ОПЕРАЦІЇ ЗА МІСЯЦЬ (БЕЗ НАКОПИЧЕНЬ) ---------
            var monthOps = _allOperations
                .Where(o =>
                    o.Currency == cur &&
                    o.CreatedAt.Year == now.Year &&
                    o.CreatedAt.Month == now.Month &&
                    (!_savingsCategoryId.HasValue || o.CategoryId != _savingsCategoryId.Value)) //  по Id, не по назві
                .ToList();

            decimal monthIncome = monthOps.Where(o => o.Amount > 0).Sum(o => o.Amount);
            decimal monthExpense = monthOps.Where(o => o.Amount < 0).Sum(o => o.Amount);

            MonthIncomeText.Text = "+ " + monthIncome.ToString("0.##", culture);

            if (monthExpense == 0)
                MonthExpenseText.Text = "- 0";
            else
                MonthExpenseText.Text = monthExpense.ToString("0.##", culture);

            MonthOperationsCountText.Text = monthOps.Count.ToString();

            // --------- ОСТАННЯ ОПЕРАЦІЯ ---------
            var last = _allOperations
                .Where(o => o.Currency == cur)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefault();

            if (last != null)
            {
                string sign = last.Amount >= 0 ? "+" : "";
                LastOperationText.Text =
                    sign + last.Amount.ToString("0.##", culture) +
                    " " + last.Currency + " • " + last.CategoryName;

                LastOperationDateText.Text = last.CreatedAt.ToString("dd.MM.yyyy HH:mm");
            }
            else
            {
                LastOperationText.Text = "—";
                LastOperationDateText.Text = "—";
            }

            // --------- БЛОК "НАКОПИЧЕННЯ" ---------
            if (GoalInfoText != null)
            {
                decimal savedTotal = 0m;

                if (_savingsCategoryId.HasValue) // ✅ ВИПРАВЛЕНО
                {
                    int sid = _savingsCategoryId.Value;
                    savedTotal = _allOperations
                        .Where(o => o.Currency == cur && o.CategoryId == sid)
                        .Sum(o => Math.Abs(o.Amount));
                }

                decimal goal;
                switch (cur)
                {
                    case "USD":
                        goal = _userSettings.GoalUSD;
                        break;
                    case "EUR":
                        goal = _userSettings.GoalEUR;
                        break;
                    default:
                        goal = _userSettings.GoalUAH;
                        break;
                }

                if (goal <= 0m)
                {
                    if (savedTotal <= 0m)
                        GoalInfoText.Text = "Ціль не задана.";
                    else
                        GoalInfoText.Text = $"Накопичено: {savedTotal.ToString("0.##", culture)} {cur}";
                }
                else
                {
                    decimal percent = goal > 0 ? (savedTotal / goal) * 100m : 0m;
                    if (percent > 100m) percent = 100m;

                    GoalInfoText.Text =
                        $"{savedTotal.ToString("0.##", culture)}/" +
                        $"{goal.ToString("0.##", culture)} {cur} • " +
                        $"{percent.ToString("0.##", culture)}%";
                }
            }
        }

        // ------------------ ОБРОБНИКИ ------------------

        private void CategoryAll_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilter(null);
        }

        private void CategorySummary_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id) //  по Id, не по назві
            {
                ApplyFilter(id);
            }
        }

        private void CurrencyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter(_currentCategoryFilterId);
            UpdateCategoryTotals();
            UpdateHeaderInfo();
        }

        private void Statistics_Click(object sender, RoutedEventArgs e)
        {
            var wnd = new StatisticsWindow(_allOperations, _savingsCategoryId);
            wnd.Owner = this;
            wnd.ShowDialog();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var wnd = new SettingsWindow(_userSettings);
            wnd.Owner = this;
            wnd.ShowDialog();
        }

        private void OperationsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void OperationsListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (OperationsListView.View is GridView gv && gv.Columns.Count == 5)
            {
                double total = OperationsListView.ActualWidth;
                if (total <= 0) return;

                gv.Columns[0].Width = total * 0.18;
                gv.Columns[1].Width = total * 0.22;
                gv.Columns[2].Width = total * 0.35;
                gv.Columns[3].Width = total * 0.15;
                gv.Columns[4].Width = total * 0.10;
            }
        }

        // ---------------- КНОПКИ ДЛЯ ОПЕРАЦІЙ ----------------

        private void AddOperation_Click(object sender, RoutedEventArgs e)
        {
            string cur = GetSelectedCurrency();

            Category defaultCategory = null;

            if (_currentCategoryFilterId.HasValue)
            {
                defaultCategory = _allCategories
                    .FirstOrDefault(c => c.Id == _currentCategoryFilterId.Value);
            }

            if (defaultCategory == null)
            {
                defaultCategory = _allCategories
                    .FirstOrDefault(c => c.Type == OperationType.Expense)
                    ?? _allCategories.FirstOrDefault();
            }

            var newOp = new Operation
            {
                Id = _nextOperationId++,
                CategoryId = defaultCategory != null ? defaultCategory.Id : 0,
                CategoryName = defaultCategory != null ? defaultCategory.Name : null,
                Amount = 0m,
                Currency = cur,
                Description = string.Empty,
                CreatedAt = DateTime.Now
            };

            var wnd = new EditOperationWindow(newOp, _allCategories, _savingsCategoryId);
            wnd.Owner = this;
            bool? result = wnd.ShowDialog();
            if (result != true)
                return;

            // якщо це накопичення і користувач ввів ціль – зберігаємо її
            if (_savingsCategoryId.HasValue && newOp.CategoryId == _savingsCategoryId.Value && wnd.SavingsGoal.HasValue) // по Id
            {
                decimal goal = wnd.SavingsGoal.Value;
                switch (newOp.Currency)
                {
                    case "UAH": _userSettings.GoalUAH = goal; break;
                    case "USD": _userSettings.GoalUSD = goal; break;
                    case "EUR": _userSettings.GoalEUR = goal; break;
                }

                SaveGoalsToDb();
            }

            _allOperations.Add(newOp);

            decimal bal = _userSettings.GetBalanceForCurrency(newOp.Currency);
            _userSettings.SetBalanceForCurrency(newOp.Currency, bal + newOp.Amount);

            // точково зберігаємо тільки нову операцію
            AddOperationToDb(newOp);

            UpdateCategoryTotals();
            ApplyFilter(_currentCategoryFilterId);
        }

        private void EditOperation_Click(object sender, RoutedEventArgs e)
        {
            var op = OperationsListView.SelectedItem as Operation;
            if (op == null)
            {
                MessageBox.Show("Спочатку оберіть операцію у списку.",
                                "Редагування операції",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                return;
            }

            decimal oldAmount = op.Amount;
            string oldCurrency = op.Currency;

            var wnd = new EditOperationWindow(op, _allCategories, _savingsCategoryId);
            wnd.Owner = this;
            bool? result = wnd.ShowDialog();
            if (result != true)
                return;

            // якщо це накопичення і користувач оновив ціль – зберігаємо її
            if (_savingsCategoryId.HasValue && op.CategoryId == _savingsCategoryId.Value && wnd.SavingsGoal.HasValue) //  по Id
            {
                decimal goal = wnd.SavingsGoal.Value;
                switch (op.Currency)
                {
                    case "UAH": _userSettings.GoalUAH = goal; break;
                    case "USD": _userSettings.GoalUSD = goal; break;
                    case "EUR": _userSettings.GoalEUR = goal; break;
                }

                SaveGoalsToDb();
            }

            // ОНОВЛЮЄМО БАЛАНСИ
            if (op.Currency == oldCurrency)
            {
                decimal bal = _userSettings.GetBalanceForCurrency(oldCurrency);
                bal = bal - oldAmount + op.Amount;
                _userSettings.SetBalanceForCurrency(oldCurrency, bal);
            }
            else
            {
                decimal balOld = _userSettings.GetBalanceForCurrency(oldCurrency);
                _userSettings.SetBalanceForCurrency(oldCurrency, balOld - oldAmount);

                decimal balNew = _userSettings.GetBalanceForCurrency(op.Currency);
                _userSettings.SetBalanceForCurrency(op.Currency, balNew + op.Amount);
            }

            UpdateCategoryTotals();
            ApplyFilter(_currentCategoryFilterId);
            OperationsListView.Items.Refresh();

            // точково оновлюємо тільки цю операцію
            UpdateOperationInDb(op);
        }

        private void DeleteOperation_Click(object sender, RoutedEventArgs e)
        {
            var op = OperationsListView.SelectedItem as Operation;
            if (op == null)
            {
                MessageBox.Show("Спочатку оберіть операцію у списку.",
                                "Видалення операції",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                "Видалити обрану операцію?",
                "Підтвердження",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            decimal bal = _userSettings.GetBalanceForCurrency(op.Currency);
            _userSettings.SetBalanceForCurrency(op.Currency, bal - op.Amount);

            _allOperations.Remove(op);
            Operations.Remove(op);

            UpdateCategoryTotals();
            ApplyFilter(_currentCategoryFilterId);

            //точково видаляємо тільки цю операцію
            DeleteOperationFromDb(op.Id);
        }

        // ---------------- КНОПКИ ДЛЯ КАТЕГОРІЙ ----------------

        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            var wnd = new AddCategoryWindow();
            wnd.Owner = this;

            if (wnd.ShowDialog() == true)
            {
                var newCategory = new Category
                {
                    Id = _nextCategoryId++,
                    Name = wnd.CategoryName,
                    Emoji = wnd.CategoryEmoji,
                    Type = wnd.CategoryType
                };

                _allCategories.Add(newCategory);

                if (newCategory.Type == OperationType.Expense)
                    ExpenseCategories.Add(newCategory);
                else
                    IncomeCategories.Add(newCategory);

                UpdateCategoryTotals();
                RefreshCategoryCollections();
                SaveCategoriesToDb();
            }
        }

        private void EditCategory_Click(object sender, RoutedEventArgs e)
        {
            if (_currentCategoryFilterId == null)
            {
                MessageBox.Show("Спочатку оберіть категорію (натисніть на іконку категорії).",
                                "Редагування категорії",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                return;
            }

            Category cat = _allCategories.FirstOrDefault(c => c.Id == _currentCategoryFilterId.Value);
            if (cat == null)
                return;

            var wnd = new AddCategoryWindow(cat);
            wnd.Owner = this;
            bool? result = wnd.ShowDialog();
            if (result != true)
                return;

            int i1 = ExpenseCategories.IndexOf(cat);
            if (i1 >= 0)
            {
                ExpenseCategories.RemoveAt(i1);
                ExpenseCategories.Insert(i1, cat);
            }

            int i2 = IncomeCategories.IndexOf(cat);
            if (i2 >= 0)
            {
                IncomeCategories.RemoveAt(i2);
                IncomeCategories.Insert(i2, cat);
            }

            foreach (var op in _allOperations.Where(o => o.CategoryId == cat.Id))
                op.CategoryName = cat.Name;

            OperationsListView.Items.Refresh();
            RefreshCategoryCollections();
            SaveCategoriesToDb();
        }

        private void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (_currentCategoryFilterId == null)
            {
                MessageBox.Show("Спочатку оберіть категорію (натисніть на іконку категорії).",
                                "Видалення категорії",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                return;
            }

            Category cat = _allCategories.FirstOrDefault(c => c.Id == _currentCategoryFilterId.Value);
            if (cat == null)
                return;

            if (cat.Name == "Накопичення")
            {
                MessageBox.Show("Категорію \"Накопичення\" видалити не можна.",
                                "Обмеження",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Видалити категорію \"{cat.Name}\" і всі операції цієї категорії?",
                "Підтвердження",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            var opsToRemove = _allOperations.Where(o => o.CategoryId == cat.Id).ToList();
            foreach (var op in opsToRemove)
            {
                decimal bal = _userSettings.GetBalanceForCurrency(op.Currency);
                _userSettings.SetBalanceForCurrency(op.Currency, bal - op.Amount);

                _allOperations.Remove(op);
                Operations.Remove(op);
            }

            _allCategories.Remove(cat);
            ExpenseCategories.Remove(cat);
            IncomeCategories.Remove(cat);

            _currentCategoryFilterId = null;

            UpdateCategoryTotals();
            ApplyFilter(null);
            RefreshCategoryCollections();
            SaveCategoriesToDb();

            // точково видаляємо з БД тільки операції цієї категорії
            DeleteOperationsByCategoryFromDb(cat.Id);
        }
    }
}
