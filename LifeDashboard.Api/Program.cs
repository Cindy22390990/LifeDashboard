using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using LifeDashboard.Infrastructure.Data;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Infrastructure.Repositories;
using LifeDashboard.Application.Services.Interfaces;
using LifeDashboard.Application.Services;

namespace LifeDashboard.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            // 1.註冊資料庫(這裡先使用 InMemory 記憶體資料庫方便你馬上測 Swagger)
            // 1. 註冊資料庫 (使用你 appsettings.json 中的 DefaultConnection)
            builder.Services.AddDbContext<LifeDashboardDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            // 2.註冊 Repository 與 Service(依賴注入)
            builder.Services.AddScoped<ISettingRepository, SettingRepository>();
            builder.Services.AddScoped<ISettingService, SettingService>();
            // 加入 Swagger 文件產生器。
            builder.Services.AddSwaggerGen(options =>
            {
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

            // 加入 OpenAPI 文件功能。
            builder.Services.AddOpenApi();

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
