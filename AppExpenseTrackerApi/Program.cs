using AppExpenseTrackerApi.ViewModelValidator;
using FluentValidation;
using Hangfire;
using Hangfire.Dashboard.BasicAuthorization;
using Hangfire.SqlServer;
using Infrastructure;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using System.Data;

namespace AppExpenseTrackerApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Serilog.Debugging.SelfLog.Enable(msg => Console.WriteLine(msg));
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddEnvironmentVariables().Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");


            // ================================
            // Request / Info Logs
            // ================================
            var requestLogColumns = new ColumnOptions();

            // 1. Remove TimeStamp so SQL Server can use its IST DEFAULT constraint
            requestLogColumns.Store.Remove(StandardColumn.TimeStamp);

            requestLogColumns.Store.Remove(StandardColumn.MessageTemplate);
            requestLogColumns.Store.Remove(StandardColumn.Properties);
            requestLogColumns.Store.Remove(StandardColumn.Exception);

            requestLogColumns.AdditionalColumns = new List<SqlColumn>
            {
                new SqlColumn("UserId", SqlDbType.NVarChar) { DataLength = 50, AllowNull = true },
                new SqlColumn("RoomId", SqlDbType.Int) { AllowNull = true },
                new SqlColumn("RequestUrl", SqlDbType.NVarChar) { DataLength = 500, AllowNull = true },
                new SqlColumn("StatusCode", SqlDbType.Int) { AllowNull = true },
                
                // Use -1 for NVARCHAR(MAX)
                new SqlColumn("RequestData", SqlDbType.NVarChar) { DataLength = -1, AllowNull = true },
                new SqlColumn("ResponseData", SqlDbType.NVarChar) { DataLength = -1, AllowNull = true },
                
                // Change to Float for easiest millisecond storage (13.3320)
                // Float handles decimal points perfectly without needing Precision/Scale settings
                new SqlColumn("TimeTakenMs", SqlDbType.Float) { AllowNull = true }
            };


            // ================================
            // Error Logs
            // ================================
            var errorLogColumns = new ColumnOptions();
            errorLogColumns.Store.Remove(StandardColumn.TimeStamp);
            errorLogColumns.Store.Remove(StandardColumn.MessageTemplate);
            errorLogColumns.Store.Remove(StandardColumn.Properties);

            errorLogColumns.AdditionalColumns = new List<SqlColumn>
            {
               new SqlColumn("UserId", SqlDbType.NVarChar) { DataLength = 50, AllowNull = true },
               new SqlColumn("RoomId", SqlDbType.Int) { AllowNull = true },
               new SqlColumn("RequestUrl", SqlDbType.NVarChar) { DataLength = 500, AllowNull = true },
               new SqlColumn("StatusCode", SqlDbType.Int) { AllowNull = true }
            };



            // ================================
            // Serilog Configuration
            // ================================
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()

                // ℹ️ Info + ⚠️ Warning → RequestLogs
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e =>
                        e.Level == LogEventLevel.Information ||
                        e.Level == LogEventLevel.Warning)
                    .WriteTo.MSSqlServer(
                        connectionString,
                        sinkOptions: new MSSqlServerSinkOptions
                        {
                            TableName = "RequestLogs",
                            AutoCreateSqlTable = false
                        },
                        columnOptions: requestLogColumns
                    )
                )

                // ❌ Error + 💥 Fatal → ErrorLog
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e =>
                        e.Level == LogEventLevel.Error ||
                        e.Level == LogEventLevel.Fatal)
                    .WriteTo.MSSqlServer(
                        connectionString,
                        sinkOptions: new MSSqlServerSinkOptions
                        {
                            TableName = "ErrorLog",
                            AutoCreateSqlTable = false
                        },
                        columnOptions: errorLogColumns
                    )
                )
                .CreateLogger();

            try
            {
                Log.Information("Starting ExpenseTracker API...");
                Log.Error(new Exception("Staring Exception Logging..."), "Staring Exception Logging...");

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
                builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

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

                builder.Services.AddValidatorsFromAssemblyContaining<ExpenseViewModelValidator>();

                var app = builder.Build();

                app.UseSerilogRequestLogging(options =>
                {
                    // 1. Keep this to hide "OPTIONS" noise (CORS pre-flights)
                    options.GetLevel = (httpContext, elapsed, ex) =>
                        httpContext.Request.Method == "OPTIONS" ? LogEventLevel.Verbose : LogEventLevel.Information;

                    // 2. Only use this for properties the middleware might miss (like Authentication)
                    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                    {
                        var user = httpContext.User;
                        if (user?.Identity?.IsAuthenticated == true)
                        {
                            var userId = user.Claims.FirstOrDefault(c => c.Type == "uid")?.Value ?? user.Identity.Name;
                            diagnosticContext.Set("UserId", userId);
                        }

                        // NOTE: We don't need to set RequestData/ResponseData here 
                        // because the Middleware already called diagnosticContext.Set()
                    };
                });

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
                string cronExpression = "30 21 */10 * *";
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
                app.UseMiddleware<LogEnrichmentMiddleware>(); 
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