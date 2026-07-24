using HappyWorld.HappyPlace.Realtime;
using HappyWorld.HappyPlace.Web.Hubs;
using HappyWorld.HappyPlace.Web.Json;
using HappyWorld.HappyPlace.Web.Services;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR;
using System.Threading.RateLimiting;

namespace HappyWorld.HappyPlace.WebApp {
    public class Program {
        // Fields
        private static readonly int MinWorkerThreads = 200;
        private static readonly int MinCompletionPortThreads = 200;
        private static readonly int RateLimitRequestsPerWindow = 300;
        private static readonly int RateLimitWindowSeconds = 60;

        public static void Main(string[] args) {
            ThreadPool.SetMinThreads(MinWorkerThreads, MinCompletionPortThreads);
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter()));
            builder.Services.AddSignalR();
            builder.Services.AddOpenApi();
            builder.Services.AddResponseCompression(options => {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });
            builder.Services.AddRateLimiter(options => {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress == null ? "unknown" : context.Connection.RemoteIpAddress.ToString(),
                        _ => new FixedWindowRateLimiterOptions {
                            PermitLimit = RateLimitRequestsPerWindow,
                            Window = TimeSpan.FromSeconds(RateLimitWindowSeconds)
                        }));
            });
            var app = builder.Build();
            if (app.Environment.IsDevelopment()) {
                app.MapOpenApi();
            }
            else {
                app.UseExceptionHandler(exceptionHandlerApp => {
                    exceptionHandlerApp.Run(context => {
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsync("{\"status\":\"error\"}");
                    });
                });
            }
            //app.UseHttpsRedirection();
            app.UseResponseCompression();
            if (!app.Environment.IsDevelopment()) {
                app.UseRateLimiter();
            }
            app.UseAuthorization();
            app.MapControllers();
            app.MapHub<RealtimeHub>("/hubs/realtime");
            SignalRRealtimeSender signalRRealtimeSender = new(app.Services.GetRequiredService<IHubContext<RealtimeHub>>());
            RealtimeSender.SetInitializer(() => signalRRealtimeSender);
            NotificationSweeper.Start();
            app.Run();
        }
    }
}
