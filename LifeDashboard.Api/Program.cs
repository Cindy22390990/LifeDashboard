using Microsoft.EntityFrameworkCore;
using LifeDashboard.Infrastructure.Data;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Infrastructure.Repositories;
using LifeDashboard.Application.Services.Interfaces;
using LifeDashboard.Application.Services;
using LifeDashboard.Application.Features.Dashboard.Interfaces;
using LifeDashboard.Application.Features.Dashboard.Queries;
using LifeDashboard.Application.Interfaces.Security;
using LifeDashboard.Application.Interfaces;
using LifeDashboard.Infrastructure.Security;
using LifeDashboard.Api.Middleware;
using LifeDashboard.Application.Features.V4.Interfaces;
using LifeDashboard.Application.Features.V4.Services;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace LifeDashboard.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<V4ProblemDetailsExceptionHandler>();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Frontend", policy =>
                {
                    policy.WithOrigins("http://localhost:5173")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            // 1.註冊資料庫(這裡先使用 InMemory 記憶體資料庫方便你馬上測 Swagger)
            // 1. 註冊資料庫 (使用你 appsettings.json 中的 DefaultConnection)
            builder.Services.AddDbContext<LifeDashboardDbContext>(options =>
            {
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"));

                options.EnableSensitiveDataLogging();

                options.LogTo(Console.WriteLine);
            });
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            // 2.註冊 Repository 與 Service(依賴注入)
            builder.Services.AddScoped<ISettingRepository, SettingRepository>();
            builder.Services.AddScoped<ISettingService, SettingService>();
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<IFixedCommitmentRepository, FixedCommitmentRepository>();
            builder.Services.AddScoped<IFixedCommitmentService, FixedCommitmentService>();
            builder.Services.AddScoped<IBudgetRadarService, BudgetRadarService>();
            builder.Services.AddScoped<IBudgetRadarQueryRepository, BudgetRadarQueryRepository>();
            builder.Services.AddScoped<BudgetRadarAssembler>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<IDashboardSummaryReader, DashboardSummaryReader>();
            builder.Services.AddScoped<GetDashboardSummaryQueryHandler>();
            builder.Services.AddScoped<IDashboardCalendarReader, DashboardCalendarReader>();
            builder.Services.AddScoped<GetDashboardCalendarDayQueryHandler>();
            builder.Services.AddScoped<GetDashboardCalendarMonthQueryHandler>();
            builder.Services.AddScoped<IGoalRepository, GoalRepository>();
            builder.Services.AddScoped<IGoalService, GoalService>();
            builder.Services.AddScoped<IGoalDepositService, GoalDepositService>();
            builder.Services.AddScoped<IGoalPurchaseService, GoalPurchaseService>();
            // 註冊 Account 服務
            builder.Services.AddScoped<IAccountRepository, AccountRepository>();
            builder.Services.AddScoped<IAccountService, AccountService>();
            builder.Services.AddScoped<IAccountDebitService, AccountDebitService>();
            builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
            builder.Services.AddScoped<ITransferCore, TransferCore>();
            builder.Services.AddScoped<ITransactionService, TransactionService>();
            builder.Services.AddScoped<IReportRepository, ReportRepository>();
            builder.Services.AddScoped<IReportService, ReportService>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IUserPreferenceRepository, UserPreferenceRepository>();
            builder.Services.AddScoped<IV4HomeReader, V4HomeReader>();
            builder.Services.AddScoped<IV4MeAssetsReader, V4MeAssetsReader>();
            builder.Services.AddScoped<IV4AccountsReader, V4AccountsReader>();
            builder.Services.AddScoped<IV4CategoryReader, V4CategoryReader>();
            builder.Services.AddScoped<IV4TransactionReader, V4TransactionReader>();
            builder.Services.AddScoped<IDailyBudgetStrategy, AccumulatedDailyBudgetStrategy>();
            builder.Services.AddScoped<IV4HomeService, V4HomeService>();
            builder.Services.AddScoped<IV4MeAssetsService, V4MeAssetsService>();
            builder.Services.AddScoped<IV4AccountsService, V4AccountsService>();
            builder.Services.AddScoped<IV4CategoriesService, V4CategoriesService>();
            builder.Services.AddScoped<IV4DashboardSummaryService, V4DashboardSummaryService>();
            builder.Services.AddScoped<IV4TransactionService, V4TransactionService>();
            builder.Services.AddScoped<ITransactionDefaultResolver, TransactionDefaultResolver>();
            builder.Services.AddScoped<UserPreferenceValidator>();
            builder.Services.AddScoped<IUserPreferenceService, UserPreferenceService>();
            // 密碼加密
            builder.Services.AddScoped<IPasswordHasher,PasswordHasher>();
            // JWT Token產生
            builder.Services.AddScoped<ITokenGenerator,JwtTokenGenerator>();
            // Authentication Service
            builder.Services.AddScoped<IAuthService, AuthService>();
            // 加入 Swagger 文件產生器。
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.CustomSchemaIds(type => type.FullName!);
                options.SupportNonNullableReferenceTypes();
                options.AddSecurityDefinition(
                    "Bearer",
                    new Microsoft.OpenApi.OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        In = Microsoft.OpenApi.ParameterLocation.Header,
                        Description =
                            "輸入 JWT Token"
                    });


                options.AddSecurityRequirement(
               document =>
               {
                   return new Microsoft.OpenApi.OpenApiSecurityRequirement
                   {
                        {
                            new Microsoft.OpenApi.OpenApiSecuritySchemeReference(
                                "Bearer",
                                document
                            ),
                            new List<string>()
                        }
                   };
               });
            });
                        // ============================================
            // JWT Authentication 設定
            // 功能:
            // 1. 接收前端 Authorization: Bearer Token
            // 2. 驗證 JWT 簽章
            // 3. 驗證 Issuer / Audience
            // 4. 驗證 Token 是否過期
            // ============================================
            var jwtSettings =
                builder.Configuration.GetSection("JwtSettings");


            builder.Services.AddAuthentication(
                JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {

                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {

                            // 驗證 Token 簽章
                            ValidateIssuerSigningKey = true,

                            IssuerSigningKey =
                                new SymmetricSecurityKey(
                                    Encoding.UTF8.GetBytes(
                                        jwtSettings["SecretKey"]!
                                    )
                                ),


                            // 驗證來源
                            ValidateIssuer = true,

                            ValidIssuer =
                                jwtSettings["Issuer"],


                            // 驗證目標
                            ValidateAudience = true,

                            ValidAudience =
                                jwtSettings["Audience"],


                            // 驗證有效期限
                            ValidateLifetime = true,


                            ClockSkew =
                                TimeSpan.Zero
                        };

                });


            var app = builder.Build();

            app.UseExceptionHandler();

            // Configure the HTTP request pipeline.
                app.UseSwagger();
                app.UseSwaggerUI();

            app.UseHttpsRedirection();

            app.UseCors("Frontend");

            app.UseAuthentication();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
