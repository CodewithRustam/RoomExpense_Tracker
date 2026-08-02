using Serilog;

namespace AppExpenseTrackerApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Serilog.Debugging.SelfLog.Enable(msg => Console.WriteLine(msg));
            var configuration = new ConfigurationBuilder()
                                   .SetBasePath(AppContext.BaseDirectory)
                                   .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                                   .AddEnvironmentVariables()
                                   .Build();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            #region Serilog Configuration

            var requestLogColumns = new ColumnOptions();
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
                new SqlColumn("RequestData", SqlDbType.NVarChar) { DataLength = -1, AllowNull = true },
                new SqlColumn("ResponseData", SqlDbType.NVarChar) { DataLength = -1, AllowNull = true },
                new SqlColumn("TimeTakenMs", SqlDbType.Float) { AllowNull = true }
            };

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

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Information || e.Level == LogEventLevel.Warning)
                    .WriteTo.MSSqlServer(connectionString, sinkOptions: new MSSqlServerSinkOptions { TableName = "RequestLogs", AutoCreateSqlTable = true }, columnOptions: requestLogColumns))
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Error || e.Level == LogEventLevel.Fatal)
                    .WriteTo.MSSqlServer(connectionString, sinkOptions: new MSSqlServerSinkOptions { TableName = "ErrorLog", AutoCreateSqlTable = true }, columnOptions: errorLogColumns))
                .CreateLogger();

            #endregion

            try
            {
                Log.Information("Starting ExpenseTracker API...");

                var builder = WebApplication.CreateBuilder(args);
                builder.Host.UseSerilog();

                builder.Services.AddControllers();
                builder.Services.AddEndpointsApiExplorer();

                #region Database, Caching & Identity

                builder.Services.AddDbContextPool<AppDbContext>(options =>
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

                #endregion

                #region Dependency Injection (The Fixes)

                // 1. Repositories (Scoped)
                builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
                builder.Services.AddScoped<IRoomRepository, RoomRepository>();
                builder.Services.AddScoped<IExpenseRepository, ExpensesRepository>();
                builder.Services.AddScoped<IMemberRepository, MemberRepository>();
                builder.Services.AddScoped<ISettlementRepository, SettlementRepository>();
                builder.Services.AddScoped<IExpenseSummaryReportRepository, ExpenseSummaryReportRepository>();
                builder.Services.AddScoped<IPasswordResetLinkRepository, PasswordResetLinkRepository>();
                builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

                // 2. Mappers & Math Services (Singletons - Highly Performant)
                builder.Services.AddSingleton<IRoomMapper, RoomMapper>();
                builder.Services.AddSingleton<IExpenseMapper, ExpenseMapper>();
                builder.Services.AddSingleton<ISettlementCalculatorService, SettlementCalculatorService>();

                // 3. Cache Services (Singletons)
                builder.Services.AddSingleton<IRoomCacheService, RoomCacheService>();
                builder.Services.AddSingleton<IExpenseCacheService, ExpenseCacheService>();
                builder.Services.AddSingleton<ISettlementCacheService, SettlementCacheService>();

                // 4. Rate Limiter (Singleton for memory state)
                builder.Services.AddSingleton<IRateLimitService, RateLimitService>();

                // 5. Core Business Services (Scoped)
                builder.Services.AddScoped<IRoomServices, RoomService>();
                builder.Services.AddScoped<IExpenseServices, ExpenseService>();
                builder.Services.AddScoped<IMemberServices, MemberService>();
                builder.Services.AddScoped<ISettlementServices, SettlementService>();
                builder.Services.AddScoped<ITokenService, TokenService>();
                builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
                builder.Services.AddScoped<IPasswordResetLinkService, PasswordResetLinkService>();
                builder.Services.AddScoped<INotificationServices, NotificationNewService>();
                builder.Services.AddScoped<NotificationService>();

                // 6. Background/Notification Dispatchers (Scoped)
                builder.Services.AddScoped<IExpenseNotificationService, ExpenseNotificationService>();
                builder.Services.AddScoped<ISettlementNotificationService, SettlementNotificationService>();
                builder.Services.AddScoped<IExpenseReportJob, ExpenseReportJob>();

                // 7. Email & Validation
                builder.Services.AddSingleton(resolver => new SmtpEmailSender(builder.Configuration.GetSection("SmtpSettings").Get<SmtpSettings>()!));
                builder.Services.AddScoped<IEmailSender>(sp => sp.GetRequiredService<SmtpEmailSender>());
                builder.Services.AddValidatorsFromAssemblyContaining<ExpenseViewModelValidator>();

                #endregion

                #region Authentication, Swagger & CORS

                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowIonic", policy =>
                    {
                        policy.WithOrigins("https://localhost", "http://localhost:8100", "https://splitx-exp.netlify.app")
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    });
                });

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
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? string.Empty))
                    };
                });

                builder.Services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ExpenseTracker API", Version = "v1" });
                    var securityScheme = new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Description = "Enter JWT Bearer token **only**",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    };
                    c.AddSecurityDefinition("Bearer", securityScheme);
                    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { securityScheme, new string[] { } } });
                });

                #endregion

                #region External Services (Firebase & Hangfire)

                var firebaseJson = builder.Configuration["Firebase__ServiceAccountJson"];

                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromJson(firebaseJson),
                    ProjectId = "splitx-c010d"
                });

                builder.Services.AddHangfire(configuration => configuration
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
                    {
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.Zero,
                        UseRecommendedIsolationLevel = true,
                        DisableGlobalLocks = true
                    }));

                builder.Services.AddHangfireServer();

                #endregion

                var app = builder.Build();

                #region Middleware Pipeline

                app.UseSerilogRequestLogging(options =>
                {
                    options.GetLevel = (httpContext, elapsed, ex) =>
                        httpContext.Request.Method == "OPTIONS" ? LogEventLevel.Verbose : LogEventLevel.Information;

                    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                    {
                        var user = httpContext.User;
                        if (user?.Identity?.IsAuthenticated == true)
                        {
                            var userId = user.Claims.FirstOrDefault(c => c.Type == "uid")?.Value ?? user.Identity.Name;
                            diagnosticContext.Set("UserId", userId);
                        }
                    };
                });

                app.UseMiddleware<ExceptionHandlingMiddleware>();

                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ExpenseTracker API V1");
                    c.RoutePrefix = "swagger";
                });

                // Security Fix: Hangfire credentials pulled from appsettings.json
                var hangfireUser = builder.Configuration["HangfireSettings:User"] ?? "admin";
                var hangfirePass = builder.Configuration["HangfireSettings:Password"] ?? "admin";

                app.UseHangfireDashboard("/hangfire", new DashboardOptions
                {
                    Authorization = new[]
                    {
                        new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
                        {
                            SslRedirect = false, RequireSsl = false, LoginCaseSensitive = true,
                            Users = new [] { new BasicAuthAuthorizationUser { Login = hangfireUser, PasswordClear = hangfirePass } }
                        })
                    }
                });

                TimeZoneInfo indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                string cronExpression = "30 21 */10 * *";

                RecurringJob.AddOrUpdate<IExpenseReportJob>(
                    "monthly-expense-report",
                    job => job.ExecuteAsync(CancellationToken.None),
                    cronExpression,
                    new RecurringJobOptions { TimeZone = indiaTimeZone }
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

                #endregion
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