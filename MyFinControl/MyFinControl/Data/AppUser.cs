using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyFinControl.Data
{
    public class AppUser
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string PasswordHash { get; set; }
        public string Salt { get; set; }
        public decimal GoalUAH { get; set; }
        public decimal GoalUSD { get; set; }
        public decimal GoalEUR { get; set; }

    }
}


