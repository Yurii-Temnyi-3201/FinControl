using System.Linq;
using Microsoft.EntityFrameworkCore;
using MyFinControl;          // ← ДОДАЛИ, щоб бачити Category та Operation

namespace MyFinControl.Data
{ public class FinContext : DbContext
    { private const string DatabaseFileName = "UserData.db";
      public DbSet<Category> Categories {get; set;}
      public DbSet<Operation> Operations {get; set;}
      public DbSet<AppUser> Users {get; set;}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {    optionsBuilder.UseSqlite($"Data Source={DatabaseFileName}");
        }

        public FinContext()
        { Database.EnsureCreated();
           // SeedDefaultUser();
        }
        //для швидшого тестування є дефолтний юзер
        private void SeedDefaultUser()
        {
            if (Users.Any()) return;

            var result = PasswordHelper.CreateHash("1234");

            Users.Add(new AppUser
            {
                Login = "admin",
                PasswordHash = result.Hash,
                Salt = result.Salt,
                GoalUAH = 0m,
                GoalUSD = 0m,
                GoalEUR = 0m
            });

            SaveChanges();
        }
    }
}
