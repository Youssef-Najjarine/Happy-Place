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
                        policy.AllowAnyOrigin()
                              .AllowAnyMethod()
                              .AllowAnyHeader()
                              .AllowCredentials()
                              .SetPreflightMaxAge(TimeSpan.FromSeconds(3600));
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