using HappyWorld.HappyPlace.Email;

namespace HappyWorld.HappyPlace
{
    public class CreateUserRecordAndGetVerificationCodeTest
    {
        [Fact]
        public void CreateUserRecordAndGetVerificationCodeBasicTest()
        {
            //using var dbContext = HappyPlaceDbContext.Create();
            Random random = new Random();
            String email = "ynajjarine@gmail.com";
            String verificationCode = random.Next(100000, 1000000).ToString();
            String name = "Youssef Najjarine";
            //String password = "TestPassword!23";
            //String username = "testuser123";
            EmailVerificationNotification.Send(email, name, verificationCode);

            //String verificationCode = UserAccountRegistrar.CreateUserRecordAndGetVerificationCode(email, name, password, username, dbContext);
            //Console.WriteLine($"VERIFICATION CODE: {verificationCode}");
        }
    }
}
