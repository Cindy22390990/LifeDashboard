using LifeDashboard.Api;
using LifeDashboard.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LifeDashboard.Tests.Helpers;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string JwtSecret = "LifeDashboard.Tests.Jwt.Secret.Key.2026";
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public CustomWebApplicationFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = JwtSecret,
                ["JwtSettings:Issuer"] = "LifeDashboard.Tests",
                ["JwtSettings:Audience"] = "LifeDashboard.Tests"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<LifeDashboardDbContext>();
            services.RemoveAll<DbContextOptions<LifeDashboardDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<LifeDashboardDbContext>>();

            services.AddDbContext<LifeDashboardDbContext>(options =>
                options.UseSqlite(_connection));
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LifeDashboardDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public static string CreateJwt(Guid userId)
    {
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(JwtSecret));
        var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key,
            Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "LifeDashboard.Tests",
            audience: "LifeDashboard.Tests",
            claims:
            [
                new System.Security.Claims.Claim(
                    System.Security.Claims.ClaimTypes.NameIdentifier,
                    userId.ToString())
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
