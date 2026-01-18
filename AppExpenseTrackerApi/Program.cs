using Hangfire;
using Hangfire.Dashboard.BasicAuthorization;
using Hangfire.SqlServer;
using Serilog;

namespace AppExpenseTrackerApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Configure Serilog early
            Log.Logger = new Serilog.LoggerConfiguration()
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

                //background Services
                builder.Services.AddScoped<IExpenseReportJob, ExpenseReportJob>();

                // Repositories
                builder.Services.AddScoped<IRoomRepository, RoomRepository>();
                builder.Services.AddScoped<IExpenseRepository, ExpensesRepository>();
                builder.Services.AddScoped<IMemberRepository, MemberRepository>();
                builder.Services.AddScoped<ISettlementRepository, SettlementRepository>();
                builder.Services.AddScoped<IExpenseSummaryReportRepository, ExpenseSummaryReportRepository>();
                builder.Services.AddScoped<IPasswordResetLinkRepository, PasswordResetLinkRepository>();
                builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

                // Services
                builder.Services.AddScoped<IRoomServices, RoomService>();
                builder.Services.AddScoped<IExpenseServices, ExpenseService>();
                builder.Services.AddScoped<IMemberServices, MemberService>();
                builder.Services.AddScoped<ISettlementServices, SettlementService>();
                builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
                builder.Services.AddScoped<IPasswordResetLinkService, PasswordResetLinkService>();
                builder.Services.AddScoped<NotificationService>();

                // Email
                builder.Services.AddSingleton(resolver =>
                    new SmtpEmailSender(builder.Configuration.GetSection("SmtpSettings").Get<SmtpSettings>()));
                builder.Services.AddScoped<IEmailSender>(sp => sp.GetRequiredService<SmtpEmailSender>());
                //builder.Services.AddScoped<IEmailSender>();

                //builder.Services.AddHostedService<ExpenseSummaryReportService>();

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

                builder.Services.AddHangfire(configuration => configuration
                                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                                .UseSimpleAssemblyNameTypeSerializer()
                                .UseRecommendedSerializerSettings()
                                // Tell Hangfire to store its state in your SQL Server
                                .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
                                {
                                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                                    QueuePollInterval = TimeSpan.Zero,
                                    UseRecommendedIsolationLevel = true,
                                    DisableGlobalLocks = true
                                }));

                builder.Services.AddHangfireServer();
                var app = builder.Build();


                app.UseMiddleware<ExceptionHandlingMiddleware>();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ExpenseTracker API V1");
                    c.RoutePrefix = "swagger";
                });

                app.UseHangfireDashboard("/hangfire", new DashboardOptions
                {
                    Authorization = new[]
                                 {
                        new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
                        {
                            SslRedirect = false,
                            RequireSsl = false,
                            LoginCaseSensitive = true,
                            Users = new []
                            {
                                new BasicAuthAuthorizationUser
                                {
                                    Login = "admin",
                                    PasswordClear =  "Rustam@121"
                                }
                            }
                        })
                    }
                });
                TimeZoneInfo indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

                // CRON expression: Minute(30) Hour(21) DayOfMonth(*/3 - every 3rd day) Month(*) DayOfWeek(*)
                string cronExpression = "30 21 */3 * *";
                //string cronExpression = "51 20 18 1 0";

                var options = new RecurringJobOptions
                {
                    TimeZone = indiaTimeZone
                };
                // Tell Hangfire: "Ensure the 'ExecuteAsync' method on IExpenseReportJob runs according to this CRON schedule in this timezone."
                RecurringJob.AddOrUpdate<IExpenseReportJob>(
                    "monthly-expense-report", // Unique ID for this job
                    job => job.ExecuteAsync(CancellationToken.None),
                    cronExpression,
                    options
                );

                app.UseHttpsRedirection();

                app.UseDefaultFiles();
                app.UseStaticFiles();
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