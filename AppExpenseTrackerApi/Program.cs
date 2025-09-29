using Domain.AppUser;
using Domain.Interfaces;
using ExpenseTrakcerHepler;
using Infrastructure.Data;
using Infrastructure.Email;
using Infrastructure.Email.Config;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Services.BackgroundJobs;
using Services.Interfaces;
using Services.Management;
using Services.Management.AuthService;
using Serilog;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace AppExpenseTrackerApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Configure Serilog early
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .AddEnvironmentVariables()
                    .Build())
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                Log.Information("Starting ExpenseTracker API...");

                var builder = WebApplication.CreateBuilder(args);

                // Replace default logging with Serilog
                builder.Host.UseSerilog();

                // Add services to the container.
                builder.Services.AddControllers();
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

                builder.Services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlServer(connectionString, sqlOptions =>
                    {
                        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    });
                });

                builder.Services.AddDataProtection()
                    .PersistKeysToDbContext<AppDbContext>()
                    .SetApplicationName("AppExpenseTracker");

                builder.Services.AddMemoryCache();

                // Repositories
                builder.Services.AddScoped<IRoomRepository, RoomRepository>();
                builder.Services.AddScoped<IExpenseRepository, ExpensesRepository>();
                builder.Services.AddScoped<IMemberRepository, MemberRepository>();
                builder.Services.AddScoped<ISettlementRepository, SettlementRepository>();
                builder.Services.AddScoped<IExpenseSummaryReportRepository, ExpenseSummaryReportRepository>();
                builder.Services.AddScoped<IPasswordResetLinkRepository, PasswordResetLinkRepository>();

                // Services
                builder.Services.AddScoped<IRoomServices, RoomService>();
                builder.Services.AddScoped<IExpenseServices, ExpenseService>();
                builder.Services.AddScoped<IMemberServices, MemberService>();
                builder.Services.AddScoped<ISettlementServices, SettlementService>();
                builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
                builder.Services.AddScoped<IPasswordResetLinkService, PasswordResetLinkService>();

                // Email
                builder.Services.AddSingleton(resolver =>
                    new SmtpEmailSender(builder.Configuration.GetSection("SmtpSettings").Get<SmtpSettings>()));
                builder.Services.AddScoped<IEmailSender>(sp => sp.GetRequiredService<SmtpEmailSender>());

                builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
                builder.Services.AddHostedService<ExpenseSummaryReportService>();

                // Identity
                builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequiredLength = 6;
                })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

                var app = builder.Build();

                // Configure middleware
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseHttpsRedirection();
                app.UseAuthorization();

                // Add Serilog request logging
                app.UseSerilogRequestLogging();

                app.MapControllers();

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
