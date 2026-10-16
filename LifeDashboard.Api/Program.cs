
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
