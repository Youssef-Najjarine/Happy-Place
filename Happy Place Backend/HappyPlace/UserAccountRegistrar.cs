using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyWorld.HappyPlace;

public class UserAccountRegistrar
{
    //Methods
    public static void RegisterWithEmailAddress(String email, String name, String password)
    {
        using var dbContext = HappyPlaceDbContext.Create();
        String username = GenerateUsername(email, name, dbContext);
        String verificationCode = CreateUserRecordAndGetVerificationCode(email, name, password, username, dbContext);
        SendEmailVerificationNotification(email, verificationCode);
    }
    public static string GenerateUsername(string email, string name, HappyPlaceDbContext dbContext)
    {
        Random random = new Random();
        bool validUserName = false;
        int usernameEqualityAttempts = 0;
        int randomNumber;
        string uniqueUsername = "";

        string baseUsername = name.ToLower().Replace(" ", "");
        baseUsername = ResizeBaseUsername(baseUsername, random);

        do
        {
            randomNumber = random.Next(1, 100);
            uniqueUsername = $"{baseUsername}{randomNumber}";
            validUserName = dbContext.PendingUserAccounts.Count(field => field.Username == uniqueUsername) == 0;

            if (!validUserName) ++usernameEqualityAttempts;
            if (usernameEqualityAttempts >= 5) break;
        } while (!validUserName);

        if (usernameEqualityAttempts >= 5)
        {
            baseUsername = email.ToLower().Split("@")[0];
            baseUsername = ResizeBaseUsername(baseUsername, random);

            validUserName = false;
            usernameEqualityAttempts = 0;

            do
            {
                randomNumber = random.Next(1, 100);
                uniqueUsername = $"{baseUsername}{randomNumber}";
                validUserName = dbContext.PendingUserAccounts.Count(field => field.Username == uniqueUsername) == 0;

                if (!validUserName) ++usernameEqualityAttempts;
                if (usernameEqualityAttempts >= 5) { 
                    throw new Exception("Unable to generate unique username after multiple attempts.");
                }
            } while (!validUserName);
        }
        return uniqueUsername;
    }
    private static string ResizeBaseUsername(string baseUsername, Random random)
    {
        string newBaseUsername = baseUsername;
        if (newBaseUsername.Length >= 18)
        {
            newBaseUsername = newBaseUsername.Substring(0, 18);
        }
        else if (newBaseUsername.Length <= 3)
        {
            newBaseUsername = GenerateMinimumBaseUsername(newBaseUsername, random);
        }
        return newBaseUsername;
    }
    private static string GenerateMinimumBaseUsername(string baseUsername, Random random)
    {
        string newBaseUsername = baseUsername;
        int randomAlphabetNumber = random.Next(0, 26);
        char randomLetter = (char)('a' + randomAlphabetNumber);
        int charsToAdd = 4 - baseUsername.Length;
        newBaseUsername = newBaseUsername.PadRight(charsToAdd, randomLetter);
        return newBaseUsername;
    }
    public static String CreateUserRecordAndGetVerificationCode(String email, String name, String password, String username, HappyPlaceDbContext dbContext)
    {
        // Send a verification code to the user's email address
        // Hash Password
        // Save this record to the PendingUserAccounts table in the database with the verification code
        // Retrieve that same verification code and return it
        throw new NotImplementedException();
    }
    private static void SendEmailVerificationNotification(String email, String verificationCode)
    {
        throw new NotImplementedException();
    }
}
