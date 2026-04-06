using Coworking.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Infrastructure.Persistence.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyGlobalConfiguration(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var type = entityType.ClrType;

            if (typeof(ITrackEntity).IsAssignableFrom(type))
            {
                modelBuilder.ConfigureTrackEntities(type);
            }
        }
    }

    private static void ConfigureTrackEntities(this ModelBuilder modelBuilder, Type entityType)
    {
        modelBuilder.Entity(entityType)
            .Property(nameof(ITrackEntity.CreatedAt))
            .IsRequired();
    }
}