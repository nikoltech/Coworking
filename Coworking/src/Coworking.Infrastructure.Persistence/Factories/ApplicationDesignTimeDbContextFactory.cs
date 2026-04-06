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
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Coworking.API"); // TODO: rewrite this to be more robust

            if (Directory.Exists(basePath) is false)
            {
                throw new DirectoryNotFoundException($"The directory '{basePath}' was not found. Ensure that the path to the API project is correct.");
            }

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile("appsettings.Development.json", true)
                //.AddJsonFile("appsettings.Local.json", true)
                .AddEnvironmentVariables()
                .Build();

            return configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }
    }
}
