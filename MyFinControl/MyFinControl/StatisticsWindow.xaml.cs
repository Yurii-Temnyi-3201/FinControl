using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MyFinControl
{
    public partial class StatisticsWindow : Window
    {
        private readonly List<Operation> _allOperations;

        private readonly int? _savingsCategoryId; // ✅ ДОДАНО: щоб не залежати від назви "Накопичення"

        // Клас для елемента діаграми
        public class MonthlyStat
        {
            public string Label { get; set; }          // "03.2025"
            public decimal Income { get; set; }        // сума доходів
            public decimal Expense { get; set; }       // сума витрат (модуль)
            public double IncomePercent { get; set; }  // висота доходів
            public double ExpensePercent { get; set; } // висота витрат
        }

        // Колекція, яку бачить XAML
        public ObservableCollection<MonthlyStat> Stats { get; } =
            new ObservableCollection<MonthlyStat>();

        // ✅ ВИПРАВЛЕНО: приймаємо savingsCategoryId
        public StatisticsWindow(IEnumerable<Operation> operations, int? savingsCategoryId)
        {
            InitializeComponent();

            _allOperations = operations != null
                ? operations.ToList()
                : new List<Operation>();

            _savingsCategoryId = savingsCategoryId; // ✅

            DataContext = this;

            InitYearFilter();
            RecalculateStats();
        }

        // ✅ ВИПРАВЛЕНО: конструктор для дизайнера
        public StatisticsWindow() : this(Enumerable.Empty<Operation>(), null)
        {
        }

        // ----------------- Допоміжні методи -----------------

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

        private int? GetSelectedYear()
        {
            if (YearComboBox != null &&
                YearComboBox.SelectedItem is ComboBoxItem item)
            {
                // ✅ ДОДАНО: "Усі роки" має Tag = null
                if (item.Tag == null)
                    return null;

                if (item.Tag is int y)
                    return y;

                // якщо Tag як рядок
                if (int.TryParse(item.Tag.ToString(), out var yy))
                    return yy;
            }
            return null;
        }

        private int? GetSelectedMonth()
        {
            if (MonthComboBox != null &&
                MonthComboBox.SelectedItem is ComboBoxItem item)
            {
                // "Увесь рік"
                if (item.Tag == null || string.IsNullOrWhiteSpace(item.Tag.ToString()))
                    return null;

                if (int.TryParse(item.Tag.ToString(), out var m) && m >= 1 && m <= 12)
                    return m;
            }
            return null;
        }

        private void InitYearFilter()
        {
            if (YearComboBox == null) return;

            YearComboBox.Items.Clear();

            // ✅ ДОДАНО: пункт "Усі роки"
            YearComboBox.Items.Add(new ComboBoxItem
            {
                Content = "Усі роки",
                Tag = null,
                IsSelected = true
            });

            var years = _allOperations
                .Select(o => o.CreatedAt.Year)
                .Distinct()
                .OrderBy(y => y)
                .ToList();

            foreach (var y in years)
            {
                YearComboBox.Items.Add(new ComboBoxItem
                {
                    Content = y.ToString(),
                    Tag = y
                });
            }
        }

        // ----------------- Побудова діаграми -----------------

        private void RecalculateStats()
        {
            Stats.Clear();

            if (_allOperations == null || _allOperations.Count == 0)
                return;

            string cur = GetSelectedCurrency();
            int? year = GetSelectedYear();
            int? month = GetSelectedMonth();

            // 1) Фільтр по валюті
            IEnumerable<Operation> ops = _allOperations.Where(o => o.Currency == cur);

            // ✅ ВИПРАВЛЕНО: виключаємо "Накопичення" по CategoryId (якщо відомий Id)
            if (_savingsCategoryId.HasValue)
            {
                int sid = _savingsCategoryId.Value;
                ops = ops.Where(o => o.CategoryId != sid);
            }

            // 2) Рік
            if (year.HasValue)
            {
                int y = year.Value;
                ops = ops.Where(o => o.CreatedAt.Year == y);
            }

            var temp = new List<MonthlyStat>();

            // ---------- ВАРІАНТ А: обраний конкретний місяць ----------
            if (month.HasValue)
            {
                int m = month.Value;

                var mOps = ops.Where(o => o.CreatedAt.Month == m).ToList();

                decimal income = mOps.Where(o => o.Amount > 0).Sum(o => o.Amount);
                decimal expense = Math.Abs(mOps.Where(o => o.Amount < 0).Sum(o => o.Amount));

                string label;
                if (year.HasValue) label = $"{m:00}.{year.Value}";
                else label = $"{m:00}";

                temp.Add(new MonthlyStat
                {
                    Label = label,
                    Income = income,
                    Expense = expense
                });
            }
            // ---------- ВАРІАНТ Б: "Увесь рік" ----------
            else
            {
                // ✅ ВИПРАВЛЕНО: якщо "усі роки" — будуємо по роках і місяцях (не тільки поточний)
                // Для простоти: якщо year не вибраний, беремо всі роки й робимо групування по (Year,Month)
                if (!year.HasValue)
                {
                    var groups = ops
                        .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                        .OrderBy(g => g.Key.Year)
                        .ThenBy(g => g.Key.Month)
                        .ToList();

                    foreach (var g in groups)
                    {
                        decimal income = g.Where(o => o.Amount > 0).Sum(o => o.Amount);
                        decimal expense = Math.Abs(g.Where(o => o.Amount < 0).Sum(o => o.Amount));

                        temp.Add(new MonthlyStat
                        {
                            Label = $"{g.Key.Month:00}.{g.Key.Year}",
                            Income = income,
                            Expense = expense
                        });
                    }
                }
                else
                {
                    int y = year.Value;
                    for (int m = 1; m <= 12; m++)
                    {
                        var mOps = ops.Where(o => o.CreatedAt.Month == m).ToList();

                        decimal income = mOps.Where(o => o.Amount > 0).Sum(o => o.Amount);
                        decimal expense = Math.Abs(mOps.Where(o => o.Amount < 0).Sum(o => o.Amount));

                        temp.Add(new MonthlyStat
                        {
                            Label = $"{m:00}.{y}",
                            Income = income,
                            Expense = expense
                        });
                    }
                }
            }

            if (temp.Count == 0)
                return;

            // масштабування висот
            decimal maxAbs = temp.Max(st => Math.Max(st.Income, st.Expense));
            double max = (double)(maxAbs == 0 ? 1 : maxAbs);

            const double MAX_BAR_HEIGHT = 280.0;

            foreach (var st in temp)
            {
                st.IncomePercent = (double)st.Income / max * MAX_BAR_HEIGHT;
                st.ExpensePercent = (double)st.Expense / max * MAX_BAR_HEIGHT;

                Stats.Add(st);
            }
        }

        // ----------------- Обробники подій -----------------

        private void CurrencyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RecalculateStats();
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            RecalculateStats();
        }
    }
}
