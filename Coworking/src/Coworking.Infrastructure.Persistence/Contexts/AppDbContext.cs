using Microsoft.EntityFrameworkCore;

namespace Coworking.Infrastructure.Persistence.Contexts
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
    }
}
