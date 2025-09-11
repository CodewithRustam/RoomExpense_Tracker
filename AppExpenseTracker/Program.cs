using AppExpenseTracker.Middlewares;
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
using Serilog;
using Services.BackgroundJobs;
using Services.Interfaces;
using Services.Management;
using Services.Management.AuthService;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        Log.Logger = new LoggerConfiguration().WriteTo.Console().WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day).CreateLogger();

        builder.Host.UseSerilog();

        string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        builder.Services.AddDataProtection().PersistKeysToDbContext<AppDbContext>().SetApplicationName("AppExpenseTracker");
        //builder.Services.AddHostedService<DailyReportService>();
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

        builder.Services.AddSingleton(resolver => new SmtpEmailSender(builder.Configuration.GetSection("SmtpSettings").Get<SmtpSettings>()));

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

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.SlidingExpiration = true;
        });

        builder.Services.AddControllersWithViews().AddViewOptions(options =>
        {
            options.HtmlHelperOptions.ClientValidationEnabled = true;
        });

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Rooms}/{action=Index}/{id?}");

        app.Run();
    }
}