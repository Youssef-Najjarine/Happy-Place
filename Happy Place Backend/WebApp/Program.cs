
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers();
            builder.Services.AddOpenApi();
            var myCorsPolicy = "_myCorsPolicy";
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: myCorsPolicy,
                    policy =>
                    {
                        // Common configurations:
                        policy.AllowAnyOrigin() // Specific origins (recommended for production)
                              .AllowAnyMethod() // Allows GET, POST, etc. (or WithMethods("GET", "POST") for specifics)
                              .AllowAnyHeader() // Allows any headers (or WithHeaders("Content-Type", "Authorization") for specifics)
                              .AllowCredentials() // If needed for cookies/auth (avoid with AllowAnyOrigin due to security risks)
                              .SetPreflightMaxAge(TimeSpan.FromSeconds(3600)); // Cache preflight responses for 1 hour
                    });
            });
            var app = builder.Build();
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }
            //app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
