using System;
using System.ComponentModel;

namespace MyFinControl
{
    public enum OperationType
    {
        Income,   // Прибуток
        Expense   // Витрата
    }

    public class Category : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public OperationType Type { get; set; }

        // Емодзі-іконка, наприклад "🍔"
        public string Emoji { get; set; }

        // Підсумок по категорії
        private decimal _totalAmount;
        public decimal TotalAmount
        {
            get { return _totalAmount; }
            set
            {
                if (_totalAmount != value)
                {
                    _totalAmount = value;
                    OnPropertyChanged("TotalAmount");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Operation
    {
        public int Id { get; set; }

        public int CategoryId { get; set; }
        public string CategoryName { get; set; }

        public string Description { get; set; }
        public decimal Amount { get; set; }  // знак + / - визначає дохід / витрату
        public string Currency { get; set; }

        public DateTime CreatedAt { get; set; }   // час операції
    }

    public class UserSettings
    {
        public decimal BalanceUAH { get; set; }
        public decimal BalanceUSD { get; set; }
        public decimal BalanceEUR { get; set; }
        public decimal GoalUAH { get; set; }
        public decimal GoalUSD { get; set; }
        public decimal GoalEUR { get; set; }

        public string PasswordHash { get; set; }  // для майбутньої авторизації
        public decimal GetGoalForCurrency(string currency)
        {
            switch (currency)
            {
                case "USD":
                    return GoalUSD;
                case "EUR":
                    return GoalEUR;
                default:
                    return GoalUAH;
            }
        }

        public void SetGoalForCurrency(string currency, decimal value)
        {
            switch (currency)
            {
                case "USD":
                    GoalUSD = value;
                    break;
                case "EUR":
                    GoalEUR = value;
                    break;
                default:
                    GoalUAH = value;
                    break;
            }
        }

        public decimal GetBalanceForCurrency(string currencyCode)
        {
            switch (currencyCode)
            {
                case "UAH":
                    return BalanceUAH;
                case "USD":
                    return BalanceUSD;
                case "EUR":
                    return BalanceEUR;
                default:
                    return 0m;
            }
        }

        public void SetBalanceForCurrency(string currencyCode, decimal value)
        {
            switch (currencyCode)
            {
                case "UAH":
                    BalanceUAH = value;
                    break;
                case "USD":
                    BalanceUSD = value;
                    break;
                case "EUR":
                    BalanceEUR = value;
                    break;
            }
        }
    }
}
