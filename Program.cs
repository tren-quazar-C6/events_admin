using events_admin.Data;
using events_admin.Services;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

var connectionString = configuration.GetConnectionString("Quasar");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'Quasar' is not configured.");
}

var urls = configuration["ASPNETCORE_URLS"] ?? "http://localhost:5039";

var host = new HostBuilder()
    .ConfigureWebHost(webBuilder =>
    {
        webBuilder
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseConfiguration(configuration)
            .UseUrls(urls)
            .ConfigureServices(services =>
            {
                services.AddDbContext<QuasarDbContext>(options =>
                    options.UseMySql(
                        connectionString,
                        new MySqlServerVersion(new Version(8, 0, 36))
                    )
                );

                services.AddScoped<MetricsService>();
                services.AddScoped<EventBusinessRulesService>();
                services.AddScoped<EmployeePermissionsService>();
                services.AddScoped<NotificationTriggerService>();
                services.AddHttpClient<MetricsApiClient>();
                services.AddControllersWithViews();

                services.AddCors(options =>
                {
                    options.AddPolicy("AllowFrontend",
                        policy =>
                        {
                            policy.AllowAnyOrigin()
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                        });
                });
            })
            .Configure(app =>
            {
                if (!string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
                {
                    app.UseExceptionHandler("/Home/Error");
                    app.UseHsts();
                }

                app.UseHttpsRedirection();
                app.UseStaticFiles();
                app.UseRouting();
                app.UseCors("AllowFrontend");
                app.UseAuthorization();

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    endpoints.MapControllerRoute(
                        name: "default",
                        pattern: "{controller=Home}/{action=Index}/{id?}");
                });
            });
    })
    .Build();

await host.RunAsync();
