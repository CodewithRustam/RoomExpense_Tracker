using Domain.AppUser;
using Domain.Interfaces;
using ExpenseTrakcerHepler;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Infrastructure.Data;
using Infrastructure.Email;
using Infrastructure.Email.Config;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Services.BackgroundJobs;
using Services.Interfaces;
using Services.Management;
using Services.Management.AuthService;
using System.Text;
using Microsoft.OpenApi.Models;
using AppExpenseTrackerApi.Middlewares;

namespace AppExpenseTrackerApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Configure Serilog early
            Log.Logger = new LoggerConfiguration()
                        .Enrich.FromLogContext()
                        .Enrich.With<CustomTimestampEnricher>()   // <-- your custom timestamp
                        .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                        .WriteTo.File(
                            path: "Logs/log-.txt",
                            rollingInterval: RollingInterval.Day,
                            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
                        )
                        .CreateLogger();


            try
            {
                Log.Information("Starting ExpenseTracker API...");

                var builder = WebApplication.CreateBuilder(args);

                // Replace default logging with Serilog
                builder.Host.UseSerilog();

                // Add services
                builder.Services.AddControllers();

                // Configure Swagger with JWT Authorization
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ExpenseTracker API", Version = "v1" });

                    // Add JWT Bearer Authorization
                    var securityScheme = new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Description = "Enter JWT Bearer token **only**",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    };

                    c.AddSecurityDefinition("Bearer", securityScheme);
                    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        { securityScheme, new string[] { } }
                    });
                });

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

                // CORS
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowIonic",
                        policy =>
                        {
                            policy.WithOrigins("https://localhost", "http://localhost:8100", "https://splitx-exp.netlify.app")
                                  .AllowAnyHeader()
                                  .AllowAnyMethod()
                                  .AllowCredentials();
                        });
                });

                // JWT Authentication
                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? string.Empty))
                    };
                });
                var firebaseApp = FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile("serviceAccountKey.json"),
                    ProjectId = "splitx-c010d"
                });
                var app = builder.Build();
                app.UseMiddleware<ExceptionHandlingMiddleware>();
                // Middleware
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI(c =>
                    {
                        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ExpenseTracker API V1");
                        c.RoutePrefix = "swagger";
                    });
                }

                app.UseHttpsRedirection();
                app.UseCors("AllowIonic");
                app.UseAuthentication();
                app.UseAuthorization();
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