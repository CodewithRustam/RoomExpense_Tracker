
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

namespace AppExpenseTrackerApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddDataProtection().PersistKeysToDbContext<AppDbContext>().SetApplicationName("AppExpenseTracker");
            //builder.Services.AddHostedService<DailyReportService>();

            builder.Services.AddMemoryCache();

            builder.Services.AddScoped<IRoomRepository, RoomRepository>();
            builder.Services.AddScoped<IExpenseRepository, ExpensesRepository>();
            builder.Services.AddScoped<IMemberRepository, MemberRepository>();
            builder.Services.AddScoped<ISettlementRepository, SettlementRepository>();
            builder.Services.AddScoped<IExpenseSummaryReportRepository, ExpenseSummaryReportRepository>();
            builder.Services.AddScoped<IPasswordResetLinkRepository, PasswordResetLinkRepository>();

            builder.Services.AddScoped<IRoomServices, RoomService>();
            builder.Services.AddScoped<IExpenseServices, ExpenseService>();
            builder.Services.AddScoped<IMemberServices, MemberService>();
            builder.Services.AddScoped<ISettlementServices, SettlementService>();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddScoped<IPasswordResetLinkService, PasswordResetLinkService>();

            builder.Services.AddSingleton(resolver =>
                new SmtpEmailSender(builder.Configuration.GetSection("SmtpSettings").Get<SmtpSettings>()));

            builder.Services.AddScoped<IEmailSender>(sp => sp.GetRequiredService<SmtpEmailSender>());

            builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            builder.Services.AddHostedService<ExpenseSummaryReportService>();

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

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
