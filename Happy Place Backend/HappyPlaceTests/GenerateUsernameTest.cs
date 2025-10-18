using HappyWorld.HappyPlace.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyWorld.HappyPlace
{
    public class GenerateUsernameTest
    {
        [Fact]
        public void NameEnteredVeryLongTest()
        {
            using var dbContext = HappyPlaceDbContext.Create();
            //dbContext.PendingUserAccounts.RemoveRange(dbContext.PendingUserAccounts);
            //dbContext.PendingUserAccounts.Add(new PendingUserAccount { Id = Guid.NewGuid(), EmailAddress = "ynajjarine@gmail.com", PhoneNumber = "9497359148", DisplayName = "Youssef Najjarine", Username = "youssef.najjarine.yo", HashedPassword = "Seven74!", VerificationCode = "123456" });
            //dbContext.PendingUserAccounts.Add(new PendingUserAccount { Id = Guid.NewGuid(), EmailAddress = "ynajarine@gmail.com", PhoneNumber = "9497359148", DisplayName = "Youssef Najjarine", Username = "ynajjarine123", HashedPassword = "Seven74!", VerificationCode = "123456" });
            //dbContext.PendingUserAccounts.Add(new PendingUserAccount { Id = Guid.NewGuid(), EmailAddress = "johnsmith@gmail.com", PhoneNumber = "9497359148", DisplayName = "Youssef Najjarine", Username = "johnSmith456", HashedPassword = "Seven74!", VerificationCode = "123456" });
            //dbContext.SaveChanges();
            String username = UserAccountRegistrar.GenerateUsername("ynajjarine@gmail.com", "youssef najjarine youssef najjarine youssef najjarine youssef najjarine", dbContext);
            Console.WriteLine($"NAME VERY LONG: {username}");
        }
        [Fact]
        public void NameEnteredVeryShortTest()
        {
            using var dbContext = HappyPlaceDbContext.Create();
            String username = UserAccountRegistrar.GenerateUsername("ynajjarine@gmail.com", "Y", dbContext);
            Console.WriteLine($"NAME VERY SHORT: {username}");
        }
        [Fact]
        public void NameEnteredMatchesExistingUsernameTest()
        {
            using var dbContext = HappyPlaceDbContext.Create();
            String username = UserAccountRegistrar.GenerateUsername("ynajjarine@gmail.com", "ynajjarine123", dbContext);
            Console.WriteLine($"NAME ENTERED MATCHES EXISTING USERNAME: {username}");
        }
    }
}
