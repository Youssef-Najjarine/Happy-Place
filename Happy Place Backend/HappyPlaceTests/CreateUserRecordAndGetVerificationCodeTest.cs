using HappyWorld.HappyPlace.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyWorld.HappyPlace
{
    public class CreateUserRecordAndGetVerificationCodeTest
    {
        [Fact]
        public void CreateUserRecordAndGetVerificationCodeBasicTest()
        {
            using var dbContext = HappyPlaceDbContext.Create();
            String email = "ynajjarine@gmail.com";
            String name = "Test User";
            String password = "TestPassword!23";
            String username = "testuser123";
            String verificationCode = UserAccountRegistrar.CreateUserRecordAndGetVerificationCode(email, name, password, username, dbContext);
            Console.WriteLine($"VERIFICATION CODE: {verificationCode}");
        }
    }
}
