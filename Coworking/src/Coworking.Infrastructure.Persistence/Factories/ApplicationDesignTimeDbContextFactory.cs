using Coworking.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Coworking.Infrastructure.Persistence.Factories
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var connectionString = GetConnectionString();

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            optionsBuilder.UseNpgsql(connectionString);

            return new AppDbContext(optionsBuilder.Options);
        }

        private static string GetConnectionString()
        {
            var apiPath = FindProjectDirectory("Coworking.API");

            if (Directory.Exists(apiPath) is false)
                throw new DirectoryNotFoundException($"API project not found: {apiPath}");

            var environment =
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? "Development";

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(apiPath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile(
                    $"appsettings.{environment}.json",
                    optional: true)
                //.AddJsonFile("appsettings.Local.json", true)
                .AddEnvironmentVariables()
                .Build();

            return configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }
        private static string FindProjectDirectory(string projectName)
        {
            var root = GetSolutionRoot();

            var dir = Directory
                .GetDirectories(root, projectName, SearchOption.AllDirectories)
                .FirstOrDefault();

            return dir ?? throw new DirectoryNotFoundException(projectName);
        }

        private static string GetSolutionRoot()
        {
            var current = new DirectoryInfo(Directory.GetCurrentDirectory());

            while (current is not null)
            {
                var hasSolution =
                    current.GetFiles("*.slnx").Any() ||
                    current.GetFiles("*.sln").Any();

                if (hasSolution)
                    return current.FullName;

                current = current.Parent;
            }

            throw new DirectoryNotFoundException(
                "Solution file (.slnx or .sln) not found.");
        }


    }
}
